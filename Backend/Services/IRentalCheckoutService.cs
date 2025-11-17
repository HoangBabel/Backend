using Backend.Data;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using static Backend.Helpers.VoucherCalculator;
using static Backend.Helpers.VoucherValidator;

namespace Backend.Services
{
    public interface IRentalCheckoutService
    {
        Task<CheckoutRentalResponse> CheckoutRentalAsync(
            int userId,
            CheckoutRentalRequest req,
            CancellationToken ct);
    }

    public class RentalCheckoutService : IRentalCheckoutService
    {
        private readonly AppDbContext _context;
        private readonly IShippingService _shippingService;
        private readonly IPayOSService _payOs;

        public RentalCheckoutService(
            AppDbContext context,
            IShippingService shippingService,
            IPayOSService payOs)
        {
            _context = context;
            _shippingService = shippingService;
            _payOs = payOs;
        }

        private static long ToVnd(decimal money)
            => (long)decimal.Round(money, 0, MidpointRounding.AwayFromZero);

        public async Task<CheckoutRentalResponse> CheckoutRentalAsync(
            int userId,
            CheckoutRentalRequest req,
            CancellationToken ct)
        {
            var rental = await _context.Rentals
                .Include(r => r.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == req.RentalId && r.UserId == userId, ct);

            if (rental == null)
                throw new InvalidOperationException("Không tìm thấy đơn thuê của người dùng này.");

            if (rental.Status != RentalStatus.Pending)
                throw new InvalidOperationException("Chỉ có thể thanh toán đơn thuê đang ở trạng thái Pending.");

            rental.RecalculateTotal();
            rental.SnapshotDepositFromItems();

            var subtotal = rental.TotalPrice;
            var deposit = rental.DepositPaid;

            // ===== SHIPPING =====
            int totalWeight = req.Weight ?? 0;
            if (totalWeight == 0)
            {
                int totalItems = rental.Items.Sum(i => i.Quantity);
                totalWeight = Math.Clamp(totalItems * 200, 200, 30000);
            }

            var shippingRequest = new ShippingFeeRequest
            {
                ToDistrictId = req.ToDistrictId,
                ToWardCode = req.ToWardCode,
                ServiceId = req.ServiceId,
                Weight = totalWeight,
                Length = req.Length ?? 20,
                Width = req.Width ?? 20,
                Height = req.Height ?? 20,
                InsuranceValue = (int)(subtotal + deposit)
            };

            var shippingResult = await _shippingService.CalculateShippingFeeAsync(shippingRequest);
            if (!shippingResult.Success)
                throw new InvalidOperationException($"Không thể tính phí vận chuyển: {shippingResult.ErrorMessage}");

            decimal shippingFee = shippingResult.ShippingFee;

            // ===== VOUCHER =====
            Vouncher? voucher = null;
            decimal subtotalDiscount = 0m;
            decimal shippingDiscount = 0m;
            decimal totalDiscount = 0m;

            if (!string.IsNullOrWhiteSpace(req.VoucherCode))
            {
                var code = req.VoucherCode.Trim();
                voucher = await _context.Vounchers.FirstOrDefaultAsync(v => v.Code == code, ct)
                          ?? throw new InvalidOperationException("Mã voucher không tồn tại.");

                if (!VoucherValidator.IsUsable(voucher, subtotal, out string errorMessage))
                    throw new InvalidOperationException(errorMessage);

                var discountResult = VoucherCalculator.CalcDiscount(voucher, subtotal, shippingFee);
                subtotalDiscount = discountResult.SubtotalDiscount;
                shippingDiscount = discountResult.ShippingDiscount;
                totalDiscount = discountResult.TotalDiscount;
            }

            var finalAmt = subtotal + deposit + shippingFee - totalDiscount;
            if (finalAmt < 0) finalAmt = 0;

            // ===== UPDATE RENTAL =====
            rental.ShippingAddress = req.ShippingAddress;
            rental.ToProvinceId = req.ToProvinceId;
            rental.ToProvinceName = req.ToProvinceName;
            rental.ToDistrictId = req.ToDistrictId;
            rental.ToDistrictName = req.ToDistrictName;
            rental.ToWardCode = req.ToWardCode;
            rental.ToWardName = req.ToWardName;
            rental.ServiceId = shippingResult.ServiceId;
            rental.ServiceType = shippingResult.ServiceType;
            rental.ShippingFee = shippingFee;
            rental.Weight = totalWeight;
            rental.Length = req.Length ?? 20;
            rental.Width = req.Width ?? 20;
            rental.Height = req.Height ?? 20;

            // ===== UPDATE VOUCHER =====
            if (voucher != null)
            {
                rental.VoucherId = voucher.Id;
                rental.VoucherCodeSnapshot = voucher.Code;
                rental.DiscountAmount = totalDiscount;

                voucher.CurrentUsageCount += 1;
                voucher.UsedAt = DateTime.UtcNow;
                if (voucher.MaxUsageCount > 0 &&
                    voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                {
                    voucher.IsValid = false;
                }
            }

            await _context.SaveChangesAsync(ct);

            var result = new CheckoutRentalResponse
            {
                RentalId = rental.Id,
                Subtotal = subtotal,
                Deposit = deposit,
                ShippingFee = shippingFee,
                SubtotalDiscount = subtotalDiscount,
                ShippingDiscount = shippingDiscount,
                Discount = totalDiscount,
                FinalAmount = finalAmt,
                PaymentMethod = req.PaymentMethod,
                VoucherCode = voucher?.Code,
                ServiceType = shippingResult.ServiceType,
                Weight = totalWeight
            };

            // ===== PAYOS =====
            if (req.PaymentMethod != PaymentMethod.QR || finalAmt <= 0m)
                return result;

            var amountVnd = ToVnd(finalAmt);

            // Reuse if exists
            var existing = await _context.Payments
                .Where(p => p.Type == PaymentType.Rental && p.RefId == rental.Id)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(ct);

            if (existing is not null && existing.Status == PaymentStatus.Created)
            {
                result.CheckoutUrl = existing.RawPayload;
                result.PaymentLinkId = existing.PaymentLinkId;
                result.QrCode = existing.QrCode;
                return result;
            }

            // New PayOS order code
            long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string desc = $"RENTAL-{rental.Id}";

            var pay = await _payOs.CreatePaymentAsync(
                orderCode,
                amountVnd,
                desc,
                ct,
                returnUrl: null
            );

            _context.Payments.Add(new Payment
            {
                OrderCode = orderCode,
                PaymentLinkId = pay.PaymentLinkId,
                Description = desc,
                Type = PaymentType.Rental,
                RefId = rental.Id,
                ExpectedAmount = amountVnd,
                Status = PaymentStatus.Created,
                CreatedAt = DateTime.UtcNow,
                RawPayload = pay.CheckoutUrl,
                QrCode = pay.QrCode
            });

            await _context.SaveChangesAsync(ct);

            result.CheckoutUrl = pay.CheckoutUrl;
            result.QrCode = pay.QrCode;
            result.PaymentLinkId = pay.PaymentLinkId;

            return result;
        }
    }
}
