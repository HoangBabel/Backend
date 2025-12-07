using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _service;
    private readonly IPayOSService _payOs;
    private readonly AppDbContext _context;

    public CheckoutController(ICheckoutService service, IPayOSService payOs, AppDbContext context)
    {
        _service = service;
        _payOs = payOs;
        _context = context;
    }

    [HttpPost("order")]
    public async Task<IActionResult> CheckoutOrder(
        [FromBody] CheckoutOrderRequest? req,
        [FromQuery] int? devUserId,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest("Body JSON rỗng hoặc sai Content-Type: application/json.");

        int userId;
        var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var uid))
            userId = uid;
        else if (devUserId.HasValue)
            userId = devUserId.Value;
        else
            return Unauthorized("Thiếu token hoặc devUserId để test.");

        try
        {
            // ----------------------------
            // 1️⃣ Tạo Order từ Service (Subtotal + Shipping)
            // ----------------------------
            var res = await _service.CheckoutOrderAsync(userId, req, ct);

            // ============================================================
            // 2️⃣ XỬ LÝ VOUCHER (nếu có req.VoucherCode)
            // ============================================================
            Vouncher? voucher = null;

            if (!string.IsNullOrWhiteSpace(req.VoucherCode))
            {
                voucher = await _context.Vounchers
                    .FirstOrDefaultAsync(v => v.Code == req.VoucherCode, ct);

                if (voucher == null)
                    return BadRequest(new { error = "Voucher không tồn tại." });

                if (!voucher.IsValid)
                    return BadRequest(new { error = "Voucher đã bị vô hiệu hóa." });

                if (voucher.ExpirationDate < DateTime.UtcNow)
                    return BadRequest(new { error = "Voucher đã hết hạn." });

                if (voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                    return BadRequest(new { error = "Voucher đã vượt quá số lần sử dụng." });

                decimal discount = 0;

                // --- 2.1: Loại discount trực tiếp (fixed) ---
                if (voucher.DiscountValue.HasValue)
                {
                    discount += voucher.DiscountValue.Value;
                }

                // --- 2.2: Loại giảm % ---
                if (voucher.DiscountPercent.HasValue)
                {
                    var percentValue = res.Subtotal * voucher.DiscountPercent.Value / 100m;

                    if (voucher.MaximumDiscount.HasValue)
                        percentValue = Math.Min(percentValue, voucher.MaximumDiscount.Value);

                    discount += percentValue;
                }

                // --- 2.3: Giảm phí ship ---
                if (voucher.ApplyToShipping)
                {
                    if (voucher.ShippingDiscountPercent.HasValue)
                    {
                        decimal shipDiscount = res.ShippingFee * (voucher.ShippingDiscountPercent.Value / 100m);
                        discount += shipDiscount;
                    }
                    else
                    {
                        // Miễn phí ship 100%
                        discount += res.ShippingFee;
                    }
                }

                // Không cho discount vượt quá tổng tiền
                res.Discount = Math.Min(discount, res.Subtotal + res.ShippingFee);

                // Tính lại FinalAmount
                res.FinalAmount = (res.Subtotal + res.ShippingFee) - res.Discount;

                // Lưu voucher code cho FE
                res.VoucherCode = voucher.Code;
            }

            // ============================================================
            // 3️⃣ LƯU lại vào Order trong DB (để Payment dùng FinalAmount mới)
            // ============================================================
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == res.OrderId, ct);

            if (order != null)
            {
                order.DiscountAmount = res.Discount;
                order.FinalAmount = res.FinalAmount;

                if (voucher != null)
                {
                    order.VoucherId = voucher.Id;
                    order.VoucherCodeSnapshot = voucher.Code;

                    voucher.CurrentUsageCount += 1;
                    if (voucher.CurrentUsageCount >= voucher.MaxUsageCount)
                        voucher.IsValid = false;
                }

                await _context.SaveChangesAsync(ct);
            }

            // ============================================================
            // 4️⃣ HANDLE PAYOS PAYMENT IF QR METHOD
            // ============================================================
            string? checkoutUrl = null;
            string? qrCode = null;
            string? paymentLinkId = null;

            if (res.PaymentMethod == PaymentMethod.QR && res.FinalAmount > 0)
            {
                var existingPayment = await _context.Payments
                    .Where(p => p.Type == PaymentType.Order && p.RefId == order!.Id && p.Status == PaymentStatus.Created)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(ct);

                PayOSCreatePaymentResult pay;

                if (existingPayment != null)
                {
                    pay = new PayOSCreatePaymentResult
                    {
                        PaymentLinkId = existingPayment.PaymentLinkId,
                        CheckoutUrl = existingPayment.RawPayload,
                        QrCode = existingPayment.QrCode
                    };
                }
                else
                {
                    var amountVnd = (long)decimal.Round(order!.FinalAmount, 0, MidpointRounding.AwayFromZero);

                    pay = await _payOs.CreatePaymentAsync(
                        order.Id,
                        amountVnd,
                        $"Đơn hàng #{order.Id}",
                        ct,
                        returnUrl: $"https://yourfrontend.com/payment-result?orderId={order.Id}"
                    );

                    _context.Payments.Add(new Payment
                    {
                        PaymentLinkId = pay.PaymentLinkId,
                        OrderCode = order.Id,
                        Description = $"Đơn hàng #{order.Id}",
                        Type = PaymentType.Order,
                        RefId = order.Id,
                        ExpectedAmount = amountVnd,
                        Status = PaymentStatus.Created,
                        CreatedAt = DateTime.UtcNow,
                        RawPayload = pay.CheckoutUrl,
                        QrCode = pay.QrCode
                    });

                    await _context.SaveChangesAsync(ct);
                }

                order.PaymentMethod = DTOs.PaymentMethod.QR;
                order.PaymentStatus = "PENDING";
                order.PaymentLinkId = pay.PaymentLinkId;
                order.PaymentUrl = pay.CheckoutUrl;
                order.QrCodeUrl = pay.QrCode;
                order.TransactionCode = pay.TransactionCode ?? Guid.NewGuid().ToString("N");

                await _context.SaveChangesAsync(ct);

                checkoutUrl = order.PaymentUrl;
                qrCode = order.QrCodeUrl;
                paymentLinkId = order.PaymentLinkId;
            }

            // ============================================================
            // 5️⃣ RETURN
            // ============================================================
            return Ok(new
            {
                res.Message,
                res.OrderId,
                res.Subtotal,
                res.ShippingFee,
                res.Discount,
                res.FinalAmount,
                PaymentMethod = res.PaymentMethod.ToString(),
                res.VoucherCode,
                res.ServiceType,
                res.Weight,
                CheckoutUrl = checkoutUrl,
                QrCode = qrCode,
                PaymentLinkId = paymentLinkId
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Lỗi khi checkout đơn hàng.", detail = ex.Message });
        }
    }
}

