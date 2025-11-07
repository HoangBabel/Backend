using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _service;
    private readonly IPayOSService _payOs;

    public CheckoutController(ICheckoutService service, IPayOSService payOs)
    {
        _service = service;
        _payOs = payOs;
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
            return Unauthorized("Thiếu token hoặc devUserId (?devUserId=1) để test.");

        try
        {
            var res = await _service.CheckoutOrderAsync(userId, req, ct);

            string? checkoutUrl = null;
            string? qrCode = null;

            if (res.PaymentMethod == PaymentMethod.QR && res.FinalAmount > 0m)
            {
                try
                {
                    var amountVnd = (long)decimal.Round(res.FinalAmount, 0, MidpointRounding.AwayFromZero);
                    var pay = await _payOs.CreatePaymentAsync(
                        res.OrderId,
                        amountVnd,
                        $"ORDER-{res.OrderId}",
                        ct
                    );
                    checkoutUrl = pay.CheckoutUrl;
                    qrCode = pay.QrCode;
                }
                catch (Exception ex)
                {
                    return Ok(new
                    {
                        res.Message,
                        res.OrderId,
                        res.Subtotal,
                        res.ShippingFee,      // ✅ Có trong Response
                        res.Discount,
                        res.FinalAmount,
                        PaymentMethod = res.PaymentMethod.ToString(),
                        res.VoucherCode,
                        res.ServiceType,      // ✅ Có trong Response
                        res.Weight,           // ✅ Có trong Response
                        Error = "Không tạo được liên kết thanh toán PayOS",
                        Detail = ex.Message
                    });
                }
            }

            return Ok(new
            {
                res.Message,
                res.OrderId,
                res.Subtotal,
                res.ShippingFee,          // ✅
                res.Discount,
                res.FinalAmount,
                PaymentMethod = res.PaymentMethod.ToString(),
                res.VoucherCode,
                res.ServiceType,          // ✅
                res.Weight,               // ✅
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
            return StatusCode(500, new { error = "Có lỗi khi checkout đơn hàng.", detail = ex.Message });
        }
    }



}
