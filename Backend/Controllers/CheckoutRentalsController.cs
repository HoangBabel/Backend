using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RentalCheckoutController : ControllerBase
{
    private readonly IRentalCheckoutService _service;
    private readonly AppDbContext _context;

    public RentalCheckoutController(IRentalCheckoutService service, AppDbContext context)
    {
        _service = service;
        _context = context;
    }

    private int GetUserId(int? devUserId)
    {
        var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim != null && int.TryParse(claim.Value, out var uid))
            return uid;

        if (devUserId.HasValue)
            return devUserId.Value;

        throw new UnauthorizedAccessException("Thiếu token hoặc devUserId.");
    }

    /// <summary>
    /// Step 1: Chỉ tạo đơn thuê — KHÔNG tạo QR
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CheckoutRental(
        [FromBody] CheckoutRentalRequest? req,
        [FromQuery] int? devUserId,
        CancellationToken ct)
    {
        if (req == null)
            return BadRequest(new { message = "Body rỗng." });

        int userId;
        try { userId = GetUserId(devUserId); }
        catch (UnauthorizedAccessException ex)
        { return Unauthorized(new { message = ex.Message }); }

        try
        {
            var res = await _service.CheckoutRentalAsync(userId, req, ct);

            return Ok(new
            {
                res.Message,
                res.RentalId,
                res.Subtotal,
                res.ShippingFee,
                res.Discount,
                res.Deposit,
                res.ServiceType,
                res.Weight,
                res.FinalAmount,

                nextStep = "CREATE_PAYMENT_LINK",
                paymentInstruction = "Gọi API /api/payos/create-rental-payment-link để tạo QR."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Lỗi khi checkout.", detail = ex.Message });
        }
    }

    /// <summary>
    /// FE kiểm tra trạng thái đơn — Không trả QR nếu chưa tạo payment link
    /// </summary>
    [HttpGet("{rentalId}/payment-status")]
    public async Task<IActionResult> GetPaymentStatus(
        int rentalId,
        [FromQuery] int? devUserId,
        CancellationToken ct)
    {
        try
        {
            int userId = GetUserId(devUserId);

            var rental = await _context.Rentals.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == rentalId && r.UserId == userId, ct);

            if (rental == null)
                return NotFound(new { message = "Không tìm thấy đơn thuê." });

            return Ok(new
            {
                rentalId = rental.Id,
                rentalStatus = rental.Status.ToString(),    // DBA-PRIMARY STATUS
                paymentStatus = rental.PaymentStatus ?? "PENDING",

                isPaymentLinkCreated = rental.PaymentLinkId != null,

                paymentUrl = rental.PaymentLinkId != null ? rental.PaymentUrl : null,
                qrCodeUrl = rental.PaymentLinkId != null ? rental.QrCodeUrl : null
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
