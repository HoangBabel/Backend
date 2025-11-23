using System.Text.Json;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/payos")]
public sealed class PayOSWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPayOSService _payOs;

    public PayOSWebhookController(AppDbContext context, IPayOSService payOs)
    {
        _context = context;
        _payOs = payOs;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] PayOSWebhookEnvelope req, CancellationToken ct)
    {
        // 1. Validate request
        if (req.data is null || string.IsNullOrEmpty(req.signature))
            return Ok(new { message = "PING_OK" });

        if (!_payOs.VerifyWebhookSignature(req.data, req.signature))
            return Ok(new { message = "IGNORED_INVALID_SIGNATURE" });

        var dataJson = JsonSerializer.Serialize(req.data);
        var data = JsonSerializer.Deserialize<PayOSWebhookData>(dataJson);
        if (data is null)
            return Ok(new { message = "OK_BAD_DATA" });

        // 2. Check success
        var isSuccess = req.success
            || string.Equals(req.code, "00", StringComparison.OrdinalIgnoreCase)
            || string.Equals(data.code, "00", StringComparison.OrdinalIgnoreCase);

        if (!isSuccess)
            return Ok(new { message = "OK_NOT_SUCCESS", reqCode = req.code, dataCode = data.code });

        // 3. Find payment
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderCode == data.orderCode, ct);

        if (payment is null)
            return Ok(new { message = "OK_PAYMENT_NOT_FOUND", orderCode = data.orderCode });

        // 4. Verify amount
        if (payment.ExpectedAmount != data.amount)
            return Ok(new
            {
                message = "OK_AMOUNT_MISMATCH",
                expected = payment.ExpectedAmount,
                received = data.amount
            });

        // 5. Update payment status
        if (payment.Status == PaymentStatus.Created)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                payment.Status = PaymentStatus.Paid;
                payment.PaidAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                // 6. Update related entity
                if (payment.Type == PaymentType.Order)
                {
                    var order = await _context.Orders.FindAsync(payment.RefId);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Completed;
                        order.PaymentStatus = "PAID";
                        order.PaidAt = DateTime.UtcNow;
                    }
                }
                else if (payment.Type == PaymentType.Rental)
                {
                    var rental = await _context.Rentals.FindAsync(payment.RefId);
                    if (rental != null && rental.Status == RentalStatus.Pending)
                    {
                        rental.Status = RentalStatus.Confirmed;
                        rental.ConfirmedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return Ok(new
                {
                    message = "OK_UPDATED",
                    paymentId = payment.Id,
                    type = payment.Type.ToString(),
                    refId = payment.RefId
                });
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        return Ok(new
        {
            message = "OK_ALREADY_PAID",
            paymentId = payment.Id,
            status = payment.Status.ToString()
        });
    }
}
