using System.Text.Json;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("api/payos")]
public class PayOSWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPayOSService _payOs;
    private readonly ILogger<PayOSWebhookController> _logger;

    public PayOSWebhookController(AppDbContext context, IPayOSService payOs, ILogger<PayOSWebhookController> logger)
    {
        _context = context;
        _payOs = payOs;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] PayOSWebhookEnvelope req, CancellationToken ct)
    {
        // A. Ping/thiếu signature
        if (req.data.ValueKind == System.Text.Json.JsonValueKind.Undefined || string.IsNullOrWhiteSpace(req.signature))
            return Ok(new { message = "PING_OK" });

        _logger.LogInformation("✅ [WEBHOOK] signature ok");

        // B. VERIFY SIGNATURE
        var raw = req.data.GetRawText();
        if (!_payOs.VerifyWebhookSignature(raw, req.signature))
        {
            _logger.LogWarning("❌ [WEBHOOK] signature invalid");
            return Ok(new { message = "IGNORED_INVALID_SIGNATURE" });
        }

        _logger.LogInformation("🔔 [WEBHOOK] HIT /api/payos/webhook");
        _logger.LogInformation("🔔 envelope: code={Code} success={Success} hasSig={HasSig}", req.code, req.success, !string.IsNullOrWhiteSpace(req.signature));

        // C. Parse data
        var data = JsonSerializer.Deserialize<PayOSWebhookData>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (data == null)
            return Ok(new { message = "OK_BAD_DATA" });

        var isSuccess = req.success
            || string.Equals(req.code, "00", StringComparison.OrdinalIgnoreCase)
            || string.Equals(data.code, "00", StringComparison.OrdinalIgnoreCase);

        if (!isSuccess)
            return Ok(new { message = "OK_NOT_SUCCESS", reqCode = req.code, dataCode = data.code });

        _logger.LogInformation("📦 [WEBHOOK] data: orderCode={OrderCode} desc={Desc} payLinkId={Pid} amount={Amount} data.code={DataCode}",
            data?.orderCode, data?.description, data?.paymentLinkId, data?.amount, data?.code);

        // D. Xác định loại giao dịch (ORDER/RENTAL)
        string descNorm = (data.description ?? "").ToUpperInvariant();
        string? kind = null;

        if (descNorm.Contains("RENTAL")) kind = "RENTAL";
        else if (descNorm.Contains("ORDER")) kind = "ORDER";

        // Fallback: suy luận thô theo DB id (chỉ là phương án cuối)
        if (kind is null)
        {
            var orderExists = await _context.Orders.AnyAsync(o => o.Id == (int)data.orderCode, ct);
            var rentalExists = await _context.Rentals.AnyAsync(r => r.Id == (int)data.orderCode, ct);
            kind = (!orderExists && rentalExists) ? "RENTAL" : "ORDER";
        }

        // E. Map Payment trước khi xử lý
        Payment? payRow = null;

        // ƯU TIÊN: paymentLinkId
        if (!string.IsNullOrWhiteSpace(data.paymentLinkId))
        {
            payRow = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PaymentLinkId == data.paymentLinkId, ct);
        }

        // Fallback: orderCode
        if (payRow == null)
        {
            payRow = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderCode == data.orderCode, ct);
        }

        if (payRow == null)
        {
            _logger.LogWarning("⚠️ [WEBHOOK] NO PAYMENT MAP: orderCode={OrderCode}, pid={Pid}", data.orderCode, data.paymentLinkId);
            return Ok(new { message = "OK_NO_PAYMENT_MAP" });
        }

        // Chuẩn hoá kind từ payRow nếu cần
        if (string.IsNullOrWhiteSpace(kind))
            kind = payRow.Type == PaymentType.Rental ? "RENTAL" : "ORDER";

        // === RENTAL ===
        if (kind == "RENTAL")
        {
            var rentalId = payRow.RefId;  // lấy từ Payments
            var rental = await _context.Rentals
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == rentalId, ct);

            if (rental == null)
                return Ok(new { message = "OK_RENTAL_NOT_FOUND", rentalId });

            // Đối soát tiền (nếu đã lưu ExpectedAmount)
            if (payRow.ExpectedAmount > 0 && payRow.ExpectedAmount != data.amount)
                return Ok(new { message = "OK_RENTAL_AMOUNT_MISMATCH", db = payRow.ExpectedAmount, payos = data.amount, rentalId });

            if (rental.Status != RentalStatus.Active)
            {
                rental.Status = RentalStatus.Active;
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("🎉 [WEBHOOK] RENTAL #{RentalId} ACTIVATED", rentalId);
            }

            await MarkPaymentPaidIfTracked(payRow.PaymentLinkId, ct);
            return Ok(new { message = "OK_RENTAL_UPDATED_ACTIVE", rentalId });
        }

        // === ORDER ===
        if (kind == "ORDER")
        {
            var orderId = payRow.RefId;
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order == null)
                return Ok(new { message = "OK_ORDER_NOT_FOUND", orderId });

            var finalVnd = (long)decimal.Round(order.FinalAmount, 0, MidpointRounding.AwayFromZero);
            if (finalVnd != data.amount)
                return Ok(new { message = "OK_ORDER_AMOUNT_MISMATCH", db = finalVnd, payos = data.amount, orderId });

            if (order.Status is OrderStatus.Pending or OrderStatus.Processing)
            {
                order.Status = OrderStatus.Completed;
                await _context.SaveChangesAsync(ct);
                await MarkPaymentPaidIfTracked(payRow.PaymentLinkId, ct);
                _logger.LogInformation("🎉 [WEBHOOK] ORDER #{OrderId} COMPLETED", orderId);
            }

            return Ok(new { message = "OK_ORDER_UPDATED", orderId });
        }

        // 🔚 Fallback cuối: nếu vì lý do gì đó không vào nhánh nào, vẫn phải return
        _logger.LogWarning("⚠️ [WEBHOOK] UNKNOWN_KIND kind={Kind}, orderCode={OrderCode}", kind, data.orderCode);
        return Ok(new { message = "OK_UNKNOWN_KIND", kind, data.orderCode });
    }




    private async Task MarkPaymentPaidIfTracked(string? paymentLinkId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(paymentLinkId)) return;

        var p = await _context.Payments.FirstOrDefaultAsync(x => x.PaymentLinkId == paymentLinkId, ct);
        if (p == null) return;

        if (p.Status != PaymentStatus.Paid)
        {
            p.Status = PaymentStatus.Paid;
            p.LastEventAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }
}

