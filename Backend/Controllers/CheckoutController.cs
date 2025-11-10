using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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


    // Controllers/CheckoutController.cs
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
            var res = await _service.CheckoutOrderAsync(userId, req, ct);

            // Lấy URL từ bảng Payments nếu là QR
            string? checkoutUrl = null;
            string? qrCode = null;

            if (res.PaymentMethod == PaymentMethod.QR)
            {
                var payment = await _context.Payments
                    .Where(p => p.Type == PaymentType.Order && p.RefId == res.OrderId)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(ct);

                checkoutUrl = payment?.RawPayload;
                qrCode = payment?.QrCode;
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
                QrCode = qrCode
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
