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

            // Tính lại tiền
            rental.RecalculateTotal();
            rental.SnapshotDepositFromItems();

            var subtotal = rental.TotalPrice;
            var deposit = rental.DepositPaid;

            // ===== BỔ SUNG SHIPPING =====

            // 1) Tính khối lượng (fallback 200g/sp)
            int totalWeight = req.Weight ?? 0;
            if (totalWeight == 0)
            {
                int totalItems = rental.Items.Sum(i => i.Quantity);
                totalWeight = Math.Clamp(totalItems * 200, 200, 30000);
            }

            // 2) Tính phí ship
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

            // 3) Áp dụng voucher
            Vouncher? voucher = null;
            decimal subtotalDiscount = 0m;
            decimal shippingDiscount = 0m;
            decimal totalDiscount = 0m;

            if (!string.IsNullOrWhiteSpace(req.VoucherCode))
            {
                var code = req.VoucherCode.Trim();
                voucher = await _context.Vounchers.FirstOrDefaultAsync(v => v.Code == code, ct)
                          ?? throw new InvalidOperationException("Mã voucher không tồn tại.");

                // ✅ Validate với error message chi tiết
                if (!VoucherValidator.IsUsable(voucher, subtotal, out string errorMessage))
                    throw new InvalidOperationException(errorMessage);

                // ✅ Tính discount cho cả subtotal và shipping
                var discountResult = VoucherCalculator.CalcDiscount(voucher, subtotal, shippingFee);
                subtotalDiscount = discountResult.SubtotalDiscount;
                shippingDiscount = discountResult.ShippingDiscount;
                totalDiscount = discountResult.TotalDiscount;
            }
            // 4) Tổng cuối cùng
            var finalAmt = subtotal + deposit + shippingFee - totalDiscount;
            if (finalAmt < 0) finalAmt = 0;

            // 5) Cập nhật thông tin shipping vào rental
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

            // 6) Cập nhật voucher
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

            // ===== KẾT THÚC SHIPPING =====

            var result = new CheckoutRentalResponse
            {
                RentalId = rental.Id,
                Subtotal = subtotal,
                Deposit = deposit,
                ShippingFee = shippingFee,
                SubtotalDiscount = subtotalDiscount,      // ✅ THÊM
                ShippingDiscount = shippingDiscount,      // ✅ THÊM
                Discount = totalDiscount,
                FinalAmount = finalAmt,
                PaymentMethod = req.PaymentMethod,
                VoucherCode = voucher?.Code,
                ServiceType = shippingResult.ServiceType,
                Weight = totalWeight
            };

            // Nếu không phải QR hoặc tiền = 0 thì khỏi tạo link
            if (req.PaymentMethod != PaymentMethod.QR || finalAmt <= 0m)
                return result;

            var amountVnd = ToVnd(finalAmt);

            // Idempotent: nếu đã có Payment "Created" cho rental này thì tái sử dụng
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

            // Tạo link mới
            var desc = $"RENTAL-{rental.Id}";
            var pay = await _payOs.CreatePaymentWithNewCodeAsync(amountVnd, desc, ct);
            var orderCodeUsed = pay.OrderCodeUsed;

            _context.Payments.Add(new Payment
            {
                PaymentLinkId = pay.PaymentLinkId!,
                OrderCode = orderCodeUsed,
                Description = desc,
                Type = PaymentType.Rental,
                RefId = rental.Id,
                ExpectedAmount = amountVnd,
                Status = PaymentStatus.Created,
                CreatedAt = DateTime.UtcNow,
                RawPayload = pay.CheckoutUrl,
                QrCode = pay.QrCode,
            });
            await _context.SaveChangesAsync(ct);

            result.CheckoutUrl = pay.CheckoutUrl;
            result.QrCode = pay.QrCode;
            result.PaymentLinkId = pay.PaymentLinkId;

            return result;
        }
    }
}
