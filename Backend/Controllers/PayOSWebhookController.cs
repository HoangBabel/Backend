using System.Text.Json;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        if (req.data is null || string.IsNullOrEmpty(req.signature))
            return Ok(new { message = "PING_OK" });

        if (!_payOs.VerifyWebhookSignature(req.data, req.signature))
            return Ok(new { message = "IGNORED_INVALID_SIGNATURE" });

        var dataJson = JsonSerializer.Serialize(req.data);
        var data = JsonSerializer.Deserialize<PayOSWebhookData>(dataJson);
        if (data is null) return Ok(new { message = "OK_BAD_DATA" });

        var isSuccess = req.success
            || string.Equals(req.code, "00", StringComparison.OrdinalIgnoreCase)
            || string.Equals(data.code, "00", StringComparison.OrdinalIgnoreCase);

        if (!isSuccess) return Ok(new
        {
            message = "OK_NOT_SUCCESS",
            reqCode = req.code,
            dataCode = data.code
        });

        var orderId = (int)data.orderCode;
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return Ok(new { message = "OK_ORDER_NOT_FOUND", orderId });

        var finalVnd = (long)decimal.Round(order.FinalAmount, 0, MidpointRounding.AwayFromZero);
        if (finalVnd != data.amount)
            return Ok(new { message = $"OK_AMOUNT_MISMATCH db={finalVnd} payos={data.amount}", orderId });

        if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Processing)
        {
            order.Status = OrderStatus.Completed;
            await _context.SaveChangesAsync(ct);
            return Ok(new { message = "OK_UPDATED", orderId });
        }

        return Ok(new { message = "OK_ALREADY_FINAL", orderId, status = order.Status.ToString() });
    }
}
