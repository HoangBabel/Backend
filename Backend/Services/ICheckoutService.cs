    using Backend.Data;
    using Backend.DTOs;
using Backend.Helpers;
using Backend.Models;
    using Microsoft.EntityFrameworkCore;
    using static Backend.Helpers.DateTimeHelper; // nếu bạn muốn dùng helper TZ VN
    using static Backend.Helpers.VoucherCalculator;
    using static Backend.Helpers.VoucherValidator;

namespace Backend.Services;

    public interface ICheckoutService
    {
        Task<CheckoutOrderResponse> CheckoutOrderAsync(int userId, CheckoutOrderRequest req, CancellationToken ct);
        //Task<CheckoutRentalResponse> CheckoutRentalByDaysAsync(CheckoutRentalByDaysRequest req, CancellationToken ct = default);
        //Task<CheckoutRentalResponse> CheckoutRentalByDatesAsync(CheckoutRentalByDatesRequest req, CancellationToken ct = default);
    }

public class CheckoutService : ICheckoutService
{
    private readonly AppDbContext _context;
    private readonly IShippingService _shippingService;
    private readonly IPayOSService _payOs;

    public CheckoutService(AppDbContext context, IShippingService shippingService, IPayOSService payOs)
    {
        _context = context;
        _shippingService = shippingService;
        _payOs = payOs;
    }

    private static long ToVnd(decimal money)
        => (long)decimal.Round(money, 0, MidpointRounding.AwayFromZero);

    private static long NewOrderCode()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var rnd = Random.Shared.Next(100, 999);
        return ts * 1000 + rnd; // tránh trùng khi gọi liên tục
    }

    public async Task<CheckoutOrderResponse> CheckoutOrderAsync(
        int userId,
        CheckoutOrderRequest req,
        CancellationToken ct)
    {
        // 1) Lấy giỏ
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Giỏ hàng trống hoặc không tồn tại.");

        // 2) Tính tiền hàng
        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        // 3) Tính khối lượng (fallback 200g/sp)
        int totalWeight = req.Weight ?? 0;
        if (totalWeight == 0)
        {
            int totalItems = cart.Items.Sum(i => i.Quantity);
            totalWeight = Math.Clamp(totalItems * 200, 200, 30000);
        }

        // 4) Phí ship
        var shippingRequest = new ShippingFeeRequest
        {
            ToDistrictId = req.ToDistrictId,
            ToWardCode = req.ToWardCode,
            ServiceId = req.ServiceId,
            Weight = totalWeight,
            Length = req.Length ?? 20,
            Width = req.Width ?? 20,
            Height = req.Height ?? 20,
            InsuranceValue = (int)subtotal
        };

        var shippingResult = await _shippingService.CalculateShippingFeeAsync(shippingRequest);
        if (!shippingResult.Success)
            throw new InvalidOperationException($"Không thể tính phí vận chuyển: {shippingResult.ErrorMessage}");

        decimal shippingFee = shippingResult.ShippingFee;

        // 5) Voucher
        Vouncher? voucher = null;
        decimal discount = 0m;
        if (!string.IsNullOrWhiteSpace(req.VoucherCode))
        {
            var code = req.VoucherCode.Trim();
            voucher = await _context.Vounchers.FirstOrDefaultAsync(v => v.Code == code, ct)
                      ?? throw new InvalidOperationException("Mã voucher không tồn tại.");

            if (!VoucherValidator.IsUsable(voucher, subtotal))
                throw new InvalidOperationException("Voucher không còn hiệu lực hoặc không đạt điều kiện.");

            discount = VoucherCalculator.CalcDiscount(voucher, subtotal);
        }

        // 6) Tổng cuối
        var finalAmount = subtotal + shippingFee - discount;
        if (finalAmount < 0) finalAmount = 0;

        // 7) Lưu Order
        using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                ShippingAddress = req.ShippingAddress,
                PaymentMethod = req.PaymentMethod,
                Status = OrderStatus.Pending,

                TotalAmount = subtotal,
                ShippingFee = shippingFee,
                DiscountAmount = discount,
                FinalAmount = finalAmount,

                ToProvinceId = req.ToProvinceId,
                ToProvinceName = req.ToProvinceName,
                ToDistrictId = req.ToDistrictId,
                ToDistrictName = req.ToDistrictName,
                ToWardCode = req.ToWardCode,
                ToWardName = req.ToWardName,

                ServiceId = shippingResult.ServiceId,
                ServiceType = shippingResult.ServiceType,
                Weight = totalWeight,
                Length = req.Length ?? 20,
                Width = req.Width ?? 20,
                Height = req.Height ?? 20,

                VoucherId = voucher?.Id,
                Voucher = voucher,
                VoucherCodeSnapshot = voucher?.Code
            };

            foreach (var item in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            cart.IsCheckedOut = true;

            if (voucher != null)
            {
                voucher.CurrentUsageCount += 1;
                voucher.UsedAt = DateTime.UtcNow;
                if (voucher.MaxUsageCount > 0 &&
                    voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                {
                    voucher.IsValid = false;
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(ct);

            // 8) Nếu QR → tạo/ghi Payment (KHÔNG return ở đây)
            if (req.PaymentMethod == PaymentMethod.QR && finalAmount > 0m)
            {
                // Idempotent: nếu đã có Payment 'Created' → tái dùng
                var existing = await _context.Payments
                    .Where(p => p.Type == PaymentType.Order && p.RefId == order.Id && p.Status == PaymentStatus.Created)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(ct);

                if (existing == null)
                {
                    var orderCode = NewOrderCode();
                    var amountVnd = ToVnd(finalAmount);
                    var desc = $"ORDER-{order.Id}";

                    PayOSCreatePaymentResult pay;

                    try
                    {
                        pay = await _payOs.CreatePaymentAsync(orderCode, amountVnd, desc, ct);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("code=231"))
                    {
                        // trùng orderCode → sinh mới và thử lại 1 lần
                        orderCode = NewOrderCode();
                        pay = await _payOs.CreatePaymentAsync(orderCode, amountVnd, desc, ct);
                    }

                    _context.Payments.Add(new Payment
                    {
                        PaymentLinkId = pay.PaymentLinkId,
                        OrderCode = orderCode,
                        Description = desc,
                        Type = PaymentType.Order,
                        RefId = order.Id,
                        ExpectedAmount = amountVnd,
                        Status = PaymentStatus.Created,
                        CreatedAt = DateTime.UtcNow,
                        RawPayload = pay.CheckoutUrl
                    });

                    await _context.SaveChangesAsync(ct);
                }
            }

            await tx.CommitAsync(ct);

            // 9) RETURN MỘT LẦN Ở CUỐI HÀM
            return new CheckoutOrderResponse
            {
                Message = "Đặt hàng thành công.",
                OrderId = order.Id,
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Discount = discount,
                FinalAmount = finalAmount,
                PaymentMethod = req.PaymentMethod,
                VoucherCode = voucher?.Code,
                ServiceType = shippingResult.ServiceType,
                Weight = totalWeight
            };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}





