using Backend.Data;
using Backend.DTOs;
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
        private readonly IPayOSService _payOs;

        public RentalCheckoutService(AppDbContext context, IPayOSService payOs)
        {
            _context = context;
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
            var finalAmt = subtotal + deposit; // hoặc chỉ deposit nếu bạn thu cọc trước
            await _context.SaveChangesAsync(ct);

            var result = new CheckoutRentalResponse
            {
                RentalId = rental.Id,
                Subtotal = subtotal,
                Deposit = deposit,
                FinalAmount = finalAmt,
                PaymentMethod = req.PaymentMethod,
            };

            // Nếu không phải QR hoặc tiền = 0 thì khỏi tạo link
            if (req.PaymentMethod != PaymentMethod.QR || finalAmt <= 0m)
                return result;

            var amountVnd = ToVnd(finalAmt);

            // Idempotent: nếu đã có Payment “Created” cho rental này thì tái sử dụng
            var existing = await _context.Payments
                .Where(p => p.Type == PaymentType.Rental && p.RefId == rental.Id)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(ct);

            if (existing is not null && existing.Status == PaymentStatus.Created)
            {
                result.CheckoutUrl = existing.RawPayload;      // nếu bạn đã lưu CheckoutUrl ở RawPayload
                result.PaymentLinkId = existing.PaymentLinkId;
                // QrCode có thể không lưu — tuỳ bạn
                return result;
            }

            // Tạo link mới (quan trọng: orderCode = rental.Id, description = "RENTAL")
            var desc = $"RENTAL-{rental.Id}";
            var pay = await _payOs.CreatePaymentWithNewCodeAsync(amountVnd, desc, ct);
            var orderCodeUsed = pay.OrderCodeUsed; // truy cập thuộc tính

            _context.Payments.Add(new Payment
            {
                PaymentLinkId = pay.PaymentLinkId!,
                OrderCode = orderCodeUsed,     // ✅ LƯU CODE THỰC SỰ ĐÃ DÙNG
                Description = desc,              // ví dụ: RENTAL-8
                Type = PaymentType.Rental,
                RefId = rental.Id,
                ExpectedAmount = amountVnd,
                Status = PaymentStatus.Created,
                CreatedAt = DateTime.UtcNow,
                RawPayload = pay.CheckoutUrl,
                QrCode      = pay.QrCode,        // nếu có cột
            });
            await _context.SaveChangesAsync(ct);

            result.CheckoutUrl = pay.CheckoutUrl;
            result.QrCode = pay.QrCode;
            result.PaymentLinkId = pay.PaymentLinkId;

            return result;
        }
    }
}
