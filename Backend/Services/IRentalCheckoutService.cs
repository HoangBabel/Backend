using Backend.Data;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

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

        private static long ToVnd(decimal money) =>
            (long)decimal.Round(money, 0, MidpointRounding.AwayFromZero);

        private static long NewPaymentCode()
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var rnd = Random.Shared.Next(100, 999);
            return ts * 1000 + rnd;
        }

        public RentalCheckoutService(
            AppDbContext context,
            IShippingService shippingService,
            IPayOSService payOs)
        {
            _context = context;
            _shippingService = shippingService;
            _payOs = payOs;
        }

        public async Task<CheckoutRentalResponse> CheckoutRentalAsync(
            int userId,
            CheckoutRentalRequest req,
            CancellationToken ct)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            // ===== Lấy đơn thuê =====
            var rental = await _context.Rentals
                .Include(r => r.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r =>
                    r.Id == req.RentalId && r.UserId == userId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy đơn thuê.");

            if (rental.Status != RentalStatus.Pending)
                throw new InvalidOperationException("Đơn thuê không hợp lệ để Checkout.");

            rental.RecalculateTotal();
            rental.SnapshotDepositFromItems();

            var subtotal = rental.TotalPrice;
            var deposit = rental.DepositPaid;

            // ===== SHIPPING =====
            int totalWeight = req.Weight ?? (rental.Items.Sum(i => i.Quantity) * 200);
            totalWeight = Math.Clamp(totalWeight, 200, 30000);

            var shippingReq = new ShippingFeeRequest
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

            var shippingResult = await _shippingService.CalculateShippingFeeAsync(shippingReq);
            if (!shippingResult.Success)
                throw new InvalidOperationException(
                    $"Không tính được phí ship: {shippingResult.ErrorMessage}");

            decimal shippingFee = shippingResult.ShippingFee;

            // ===== VOUCHER =====
            decimal subtotalDiscount = 0m, shippingDiscount = 0m, totalDiscount = 0m;
            Vouncher? voucher = null;

            if (!string.IsNullOrWhiteSpace(req.VoucherCode))
            {
                var code = req.VoucherCode.Trim();
                voucher = await _context.Vounchers
                    .FirstOrDefaultAsync(v => v.Code == code, ct)
                    ?? throw new InvalidOperationException("Voucher không tồn tại.");

                if (!VoucherValidator.IsUsable(voucher, subtotal, out string error))
                    throw new InvalidOperationException(error);

                var dis = VoucherCalculator.CalcDiscount(voucher, subtotal, shippingFee);
                subtotalDiscount = dis.SubtotalDiscount;
                shippingDiscount = dis.ShippingDiscount;
                totalDiscount = dis.TotalDiscount;

                rental.VoucherId = voucher.Id;
                rental.VoucherCodeSnapshot = voucher.Code;
            }

            var finalAmount = Math.Max(0m, subtotal + deposit + shippingFee - totalDiscount);

            // ===== Cập nhật snapshot giá và thông tin ship =====
            rental.PaymentMethod = req.PaymentMethod;
            rental.ShippingAddress = req.ShippingAddress;

            rental.ToProvinceId = req.ToProvinceId;
            rental.ToProvinceName = req.ToProvinceName;
            rental.ToDistrictId = req.ToDistrictId;
            rental.ToDistrictName = req.ToDistrictName;
            rental.ToWardCode = req.ToWardCode;
            rental.ToWardName = req.ToWardName;

            rental.ServiceId = shippingResult.ServiceId;
            rental.ServiceType = shippingResult.ServiceType;
            rental.Weight = totalWeight;
            rental.Length = req.Length ?? 20;
            rental.Width = req.Width ?? 20;
            rental.Height = req.Height ?? 20;

            rental.ShippingFee = shippingFee;
            rental.DiscountAmount = totalDiscount;

            rental.Status = RentalStatus.Pending;
            rental.PaymentStatus ??= "UNPAID";

            // Chốt giá trị trước khi qua thanh toán
            await _context.SaveChangesAsync(ct);

            // =====================================================
            //         QR PAYMENT FLOW (PayOS)
            // =====================================================
            string? checkoutUrl = null;
            string? qrUrl = null;
            string? linkId = null;

            if (req.PaymentMethod == PaymentMethod.QR && finalAmount > 0)
            {
                var code = NewPaymentCode();
                var desc = $"RENTAL-{rental.Id}";
                var amount = ToVnd(finalAmount);

                PayOSCreatePaymentResult pay;
                try
                {
                    pay = await _payOs.CreatePaymentAsync(code, amount, desc, ct);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("code=231"))
                {
                    code = NewPaymentCode();
                    pay = await _payOs.CreatePaymentAsync(code, amount, desc, ct);
                }

                // Lưu vào bảng Payment
                _context.Payments.Add(new Payment
                {
                    PaymentLinkId = pay.PaymentLinkId,
                    OrderCode = code,
                    Description = desc,
                    Type = PaymentType.Rental,
                    RefId = rental.Id,
                    ExpectedAmount = amount,
                    Status = PaymentStatus.Created,
                    CreatedAt = DateTime.UtcNow,
                    RawPayload = pay.CheckoutUrl,
                    QrCode = pay.QrCode
                });

                await _context.SaveChangesAsync(ct);

                // Lưu vào Rental
                rental.PaymentLinkId = pay.PaymentLinkId;
                rental.PaymentUrl = pay.CheckoutUrl;
                rental.QrCodeUrl = pay.QrCode;
                rental.PaymentStatus = "PENDING";
                rental.Status = RentalStatus.Pending; // vẫn Pending cho đến khi webhook xác nhận
                rental.PaidAt = null; // chưa thanh toán
                await _context.SaveChangesAsync(ct);

                checkoutUrl = pay.CheckoutUrl;
                qrUrl = pay.QrCode;
                linkId = pay.PaymentLinkId;
            }

            // COD giữ nguyên Pending, Unpaid
            if (req.PaymentMethod == PaymentMethod.COD)
            {
                rental.Status = RentalStatus.Pending;
                rental.PaymentStatus = "PENDING";
                await _context.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);

            return new CheckoutRentalResponse
            {
                Message = "Checkout thành công.",
                RentalId = rental.Id,
                Subtotal = subtotal,
                Deposit = deposit,
                ShippingFee = shippingFee,
                SubtotalDiscount = subtotalDiscount,
                ShippingDiscount = shippingDiscount,
                Discount = totalDiscount,
                FinalAmount = finalAmount,
                PaymentMethod = req.PaymentMethod,
                VoucherCode = voucher?.Code,
                ServiceType = shippingResult.ServiceType,
                Weight = totalWeight,
                CheckoutUrl = checkoutUrl,
                QrCode = qrUrl,
                PaymentLinkId = linkId
            };
        }
    }
}
