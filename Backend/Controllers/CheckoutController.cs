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
            // 1️⃣ Tạo Order bằng CheckoutService
            var res = await _service.CheckoutOrderAsync(userId, req, ct);

            string? checkoutUrl = null;
            string? qrCode = null;
            string? paymentLinkId = null;

            // 2️⃣ Nếu PaymentMethod là QR → tạo Payment link
            if (res.PaymentMethod == PaymentMethod.QR && res.FinalAmount > 0)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == res.OrderId, ct);
                if (order is not null)
                {
                    // ✅ Kiểm tra Payment đã tồn tại chưa
                    var existingPayment = await _context.Payments
                        .Where(p => p.Type == PaymentType.Order && p.RefId == order.Id && p.Status == PaymentStatus.Created)
                        .OrderByDescending(p => p.Id)
                        .FirstOrDefaultAsync(ct);

                    PayOSCreatePaymentResult pay;

                    if (existingPayment != null)
                    {
                        // Reuse Payment cũ
                        pay = new PayOSCreatePaymentResult
                        {
                            PaymentLinkId = existingPayment.PaymentLinkId,
                            CheckoutUrl = existingPayment.RawPayload,
                            QrCode = existingPayment.QrCode
                        };
                    }
                    else
                    {
                        // Tạo mới Payment
                        var amountVnd = (long)decimal.Round(order.FinalAmount, 0, MidpointRounding.AwayFromZero);
                        var description = $"Đơn hàng #{order.Id} - {order.User?.Email ?? "Khách hàng"}";

                        pay = await _payOs.CreatePaymentAsync(
                            order.Id,
                            amountVnd,
                            description,
                            ct,
                            returnUrl: $"https://yourfrontend.com/payment-result?orderId={order.Id}"
                        );

                        _context.Payments.Add(new Payment
                        {
                            PaymentLinkId = pay.PaymentLinkId,
                            OrderCode = order.Id,
                            Description = description,
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

                    // ✅ Lưu thông tin Payment vào Order
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
            }

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
