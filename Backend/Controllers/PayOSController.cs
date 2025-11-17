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
public sealed class PayOSController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPayOSService _payOs;
    private readonly IConfiguration _config;

    public PayOSController(AppDbContext context, IPayOSService payOs, IConfiguration config)
    {
        _context = context;
        _payOs = payOs;
        _config = config;
    }

    /// <summary>
    /// 🔗 Tạo liên kết thanh toán PayOS cho đơn hàng
    /// </summary>
    [HttpPost("create-payment-link")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkDto dto, CancellationToken ct)
    {
        if (dto.OrderId <= 0)
            return BadRequest(new { message = "OrderId không hợp lệ." });

        var order = await _context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);

        if (order is null)
            return NotFound(new { message = "Không tìm thấy đơn hàng." });

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Processing))
            return BadRequest(new { message = "Đơn hàng không ở trạng thái chờ thanh toán." });

        var amountVnd = (long)decimal.Round(order.FinalAmount, 0, MidpointRounding.AwayFromZero);
        if (amountVnd <= 0)
            return BadRequest(new { message = "Số tiền thanh toán không hợp lệ." });

        var baseUrl = _config["Frontend:BaseUrl"] ?? "https://localhost:5173";
        var returnUrl = $"{baseUrl}/payment-result?orderId={order.Id}";

        var emailPart = order.User?.Email ?? "Khách hàng";
        var desc = $"Đơn hàng #{order.Id} - {emailPart}";
        var description = desc[..Math.Min(desc.Length, 25)];

        try
        {
            var result = await _payOs.CreatePaymentAsync(order.Id, amountVnd, description, ct, returnUrl);

            order.Status = OrderStatus.Processing;
            order.PaymentMethod = DTOs.PaymentMethod.QR;
            order.PaymentStatus = "PENDING";
            order.PaymentLinkId = result.PaymentLinkId;
            order.PaymentUrl = result.CheckoutUrl;
            order.QrCodeUrl = result.QrCode;
            order.TransactionCode = result.TransactionCode ?? Guid.NewGuid().ToString("N");

            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                message = "Tạo liên kết thanh toán thành công.",
                orderId = order.Id,
                paymentUrl = result.CheckoutUrl,
                qrCode = result.QrCode,
                paymentLinkId = result.PaymentLinkId,
                status = order.PaymentStatus
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Lỗi khi tạo liên kết thanh toán PayOS.",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 📦 Xử lý webhook hoặc xác nhận thanh toán từ frontend
    /// </summary>
    [HttpPost("confirm-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPayment([FromBody] PayOSWebhookEnvelope req, CancellationToken ct)
    {
        if (req?.data is null || string.IsNullOrEmpty(req.signature))
            return BadRequest(new { message = "Thiếu dữ liệu hoặc chữ ký." });

        if (!_payOs.VerifyWebhookSignature(req.data, req.signature))
            return Unauthorized(new { message = "Chữ ký không hợp lệ." });

        var dataJson = JsonSerializer.Serialize(req.data);
        var data = JsonSerializer.Deserialize<PayOSWebhookData>(dataJson);
        if (data is null)
            return BadRequest(new { message = "Dữ liệu webhook không hợp lệ." });

        var isSuccess =
            req.success ||
            string.Equals(req.code, "00", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(data.code, "00", StringComparison.OrdinalIgnoreCase);

        var orderId = (int)data.orderCode;
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return NotFound(new { message = "Không tìm thấy đơn hàng.", orderId });

        var finalVnd = (long)decimal.Round(order.FinalAmount, 0, MidpointRounding.AwayFromZero);
        if (finalVnd != data.amount)
            return BadRequest(new { message = "Số tiền không khớp.", dbAmount = finalVnd, received = data.amount, orderId });

        // ✅ Đồng bộ trạng thái thanh toán
        if (isSuccess)
        {
            if (order.Status != OrderStatus.Completed && order.PaymentStatus != "PAID")
            {
                order.Status = OrderStatus.Completed;
                order.PaymentStatus = "PAID";
                order.PaidAt = DateTime.UtcNow; // <-- dùng DateTime, tương thích model
                order.TransactionCode = data.reference ?? data.transactionCode ?? order.TransactionCode;
                await _context.SaveChangesAsync(ct);
            }

            return Ok(new
            {
                message = "Thanh toán thành công.",
                orderId = order.Id,
                status = order.Status.ToString(),
                paymentStatus = order.PaymentStatus,
                paidAt = order.PaidAt,
                transactionCode = order.TransactionCode
            });
        }

        // ❌ Thanh toán thất bại (chỉ cập nhật nếu chưa hoàn tất)
        if (order.Status != OrderStatus.Completed)
        {
            order.PaymentStatus = "FAILED";
            await _context.SaveChangesAsync(ct);
        }

        return Ok(new
        {
            message = "Thanh toán thất bại.",
            orderId = order.Id,
            status = order.Status.ToString(),
            paymentStatus = order.PaymentStatus
        });
    }

    /// <summary>
    /// ✅ Kiểm tra trạng thái thanh toán
    /// </summary>
    [HttpGet("status/{orderId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPaymentStatus(int orderId, CancellationToken ct)
    {
        var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return NotFound(new { message = "Không tìm thấy đơn hàng." });

        return Ok(new
        {
            order.Id,
            order.Status,
            paymentStatus = order.PaymentStatus ?? "UNKNOWN",
            order.PaymentUrl,
            order.QrCodeUrl,
            order.FinalAmount,
            order.PaidAt,
            order.TransactionCode
        });
    }
}

public class CreatePaymentLinkDto
{
    public int OrderId { get; set; }
}

