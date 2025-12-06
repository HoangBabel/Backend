using System.Text.Json;
using Backend.Data;
using Backend.DTOs;
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
    private readonly ILogger<PayOSController> _logger;

    public PayOSController(
        AppDbContext context,
        IPayOSService payOs,
        IConfiguration config,
        ILogger<PayOSController> logger)
    {
        _context = context;
        _payOs = payOs;
        _config = config;
        _logger = logger;
    }

    private static long ToVnd(decimal money) =>
        (long)decimal.Round(money, 0, MidpointRounding.AwayFromZero);

    private static long NewPaymentCode()
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var rnd = Random.Shared.Next(100, 999);
        return ts * 1000 + rnd;
    }

    private int GetUserId(int? devUserId)
    {
        var claim = User.FindFirst("id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var uid))
            return uid;

        if (devUserId.HasValue)
            return devUserId.Value;

        throw new UnauthorizedAccessException("Thiếu token hoặc devUserId.");
    }

    /// <summary>
    /// 🔗 Tạo liên kết thanh toán cho đơn thuê (PayOS QR)
    /// - Idempotent: reuse Payment nếu đã tạo
    /// - Lưu Payment record vào DB
    /// - Lưu snapshot PayOS info vào Rental (PaymentLinkId, PaymentUrl, QrCodeUrl)
    /// - Đặt rental.PaymentStatus = "PENDING"
    /// </summary>
    [HttpPost("create-rental-payment-link")]
    public async Task<IActionResult> CreateRentalPaymentLink(
        [FromBody] CreateRentalPaymentLinkDto dto,
        [FromQuery] int? devUserId,
        CancellationToken ct)
    {
        if (dto == null || dto.RentalId <= 0)
            return BadRequest(new { message = "RentalId không hợp lệ." });

        int userId;
        try { userId = GetUserId(devUserId); }
        catch
        {
            return Unauthorized(new { message = "Thiếu token hoặc devUserId." });
        }

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var rental = await _context.Rentals
                .Include(r => r.Items)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == dto.RentalId && r.UserId == userId, ct);

            if (rental == null)
                return NotFound(new { message = "Không tìm thấy đơn thuê." });

            if (rental.Items == null || !rental.Items.Any())
                return BadRequest(new { message = "Đơn thuê chưa có sản phẩm." });

            // Recalculate to ensure DB is up-to-date
            rental.RecalculateTotal();
            rental.SnapshotDepositFromItems();

            var decimalAmount = rental.TotalPrice + rental.ShippingFee - (rental.DiscountAmount ?? 0m);
            var amount = ToVnd(Math.Max(0m, decimalAmount));
            if (amount <= 0)
                return BadRequest(new { message = "Số tiền không hợp lệ." });

            // Reuse payment if exists (Created)
            var existingPayment = await _context.Payments
                .Where(p => p.Type == PaymentType.Rental && p.RefId == rental.Id && p.Status == PaymentStatus.Created)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync(ct);

            PayOSCreatePaymentResult payResult = null!;
            if (existingPayment != null)
            {
                _logger.LogInformation("Reuse existing payment (id={PaymentId}) for rental {RentalId}", existingPayment.Id, rental.Id);
                payResult = new PayOSCreatePaymentResult
                {
                    CheckoutUrl = existingPayment.RawPayload ?? string.Empty,
                    QrCode = existingPayment.QrCode,
                    PaymentLinkId = existingPayment.PaymentLinkId,
                    TransactionCode = null
                };

                // Ensure rental snapshot contains info
                rental.PaymentLinkId ??= existingPayment.PaymentLinkId;
                rental.PaymentUrl ??= existingPayment.RawPayload;
                rental.QrCodeUrl ??= existingPayment.QrCode;
                rental.PaymentStatus ??= "PENDING";
                rental.PaymentMethod = PaymentMethod.QR;
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return Ok(new
                {
                    message = "Đã tồn tại link thanh toán PayOS (reuse).",
                    rentalId = rental.Id,
                    paymentUrl = rental.PaymentUrl,
                    qrCodeUrl = rental.QrCodeUrl,
                    paymentLinkId = rental.PaymentLinkId,
                    status = rental.PaymentStatus
                });
            }

            // Create new PayOS payment
            var orderCode = NewPaymentCode();
            var desc = $"RENTAL-{rental.Id}";
            var baseUrl = _config["Frontend:BaseUrl"] ?? "https://localhost:5173";
            var returnUrl = $"{baseUrl}/rental-payment-result?rentalId={rental.Id}";

            try
            {
                payResult = await _payOs.CreatePaymentAsync(orderCode, amount, desc, ct, returnUrl);
            }
            catch (InvalidOperationException ex) when (ex.Message?.Contains("231") == true || ex.Message?.Contains("\"code\":\"231\"") == true)
            {
                // Nếu PayOS báo "exists", cố parse body nếu có (fallback handled below)
                _logger.LogInformation(ex, "PayOS returned code 231 when creating payment. Trying to parse possible existing link.");
                // Try parse JSON in message (best-effort)
                string? checkoutUrl = null, qr = null, pid = null;
                try
                {
                    var msg = ex.Message ?? string.Empty;
                    var bodyIndex = msg.IndexOf("body=", StringComparison.OrdinalIgnoreCase);
                    if (bodyIndex >= 0)
                    {
                        var jsonPart = msg.Substring(bodyIndex + "body=".Length).Trim();
                        var doc = JsonDocument.Parse(jsonPart);
                        if (doc.RootElement.TryGetProperty("data", out var dataEl))
                        {
                            if (dataEl.TryGetProperty("checkoutUrl", out var cu)) checkoutUrl = cu.GetString();
                            if (dataEl.TryGetProperty("qrCode", out var qel)) qr = qel.GetString();
                            if (dataEl.TryGetProperty("paymentLinkId", out var pidEl)) pid = pidEl.GetString();
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    _logger.LogWarning(parseEx, "Không parse được body từ exception PayOS (231).");
                }

                if (!string.IsNullOrWhiteSpace(pid) || !string.IsNullOrWhiteSpace(checkoutUrl))
                {
                    // Save Payment record using extracted info (best-effort)
                    var payment = new Payment
                    {
                        PaymentLinkId = pid,
                        OrderCode = orderCode,
                        Type = PaymentType.Rental,
                        RefId = rental.Id,
                        ExpectedAmount = amount,
                        Status = PaymentStatus.Created,
                        QrCode = qr,
                        CreatedAt = DateTime.UtcNow,
                        RawPayload = checkoutUrl,
                        Description = desc
                    };
                    _context.Payments.Add(payment);

                    // Save snapshot to rental
                    rental.PaymentLinkId = pid;
                    rental.PaymentUrl = checkoutUrl;
                    rental.QrCodeUrl = qr;
                    rental.PaymentMethod = PaymentMethod.QR;
                    rental.PaymentStatus = "PENDING";
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    return Ok(new
                    {
                        message = "Đã tồn tại link thanh toán PayOS (parsed).",
                        rentalId = rental.Id,
                        paymentUrl = rental.PaymentUrl,
                        qrCodeUrl = rental.QrCodeUrl,
                        paymentLinkId = rental.PaymentLinkId,
                        status = rental.PaymentStatus
                    });
                }

                // Nếu không lấy được dữ liệu từ message -> trả lỗi nhẹ (không throw 500)
                await tx.RollbackAsync(ct);
                return StatusCode(409, new { message = "PayOS trả lỗi: link đã tồn tại nhưng không thể trích xuất.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Lỗi khi gọi PayOS CreatePaymentAsync cho rental {RentalId}", rental.Id);
                return StatusCode(500, new { message = "Lỗi khi tạo link PayOS.", error = ex.Message });
            }

            // Lưu Payment record
            var newPayment = new Payment
            {
                PaymentLinkId = payResult.PaymentLinkId,
                OrderCode = NewPaymentCode(), // lưu một mã khác nếu muốn; nhưng để đối chiếu, ta lưu mã đã dùng khi gọi PayOS - assume payResult doesn't include it => store orderCode used earlier.
                Type = PaymentType.Rental,
                RefId = rental.Id,
                ExpectedAmount = amount,
                Status = PaymentStatus.Created,
                QrCode = payResult.QrCode,
                CreatedAt = DateTime.UtcNow,
                RawPayload = payResult.CheckoutUrl,
                Description = desc
            };

            // Important: if _payOs returned a PaymentLinkId but we don't have the exact orderCode, prefer to save the orderCode we sent.
            // to keep consistent, change above to store the orderCode we requested:
            newPayment.OrderCode = NewPaymentCode(); // NOTE: better if payResult returned the orderCode; safe fallback: set to value used.
            // But to keep stable mapping, set newPayment.OrderCode = orderCode used earlier
            newPayment.OrderCode = orderCode;

            _context.Payments.Add(newPayment);

            // Update rental snapshot
            rental.PaymentLinkId = payResult.PaymentLinkId;
            rental.PaymentUrl = payResult.CheckoutUrl;
            rental.QrCodeUrl = payResult.QrCode;
            rental.PaymentStatus = "PENDING";
            rental.PaymentMethod = PaymentMethod.QR;
            // Do not set rental.Status = Active here; Wait for webhook confirmation
            await _context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            return Ok(new
            {
                message = "Tạo PayOS QR thành công.",
                rentalId = rental.Id,
                paymentUrl = rental.PaymentUrl,
                qrCodeUrl = rental.QrCodeUrl,
                paymentLinkId = rental.PaymentLinkId,
                status = rental.PaymentStatus
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Lỗi khi tạo PayOS link cho rental {RentalId}", dto.RentalId);
            return StatusCode(500, new { message = "Lỗi máy chủ.", error = ex.Message });
        }
    }

    /// <summary>
    /// 📦 Xác nhận thanh toán từ PayOS (webhook hoặc FE)
    /// - Endpoint idempotent, verify signature
    /// - Cập nhật Payment record và entity liên quan (Rental)
    /// </summary>
    [HttpPost("rental/confirm-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmRentalPayment([FromBody] PayOSWebhookEnvelope req, CancellationToken ct)
    {
        if (req?.data is null || string.IsNullOrEmpty(req.signature))
            return BadRequest(new { message = "Thiếu dữ liệu hoặc chữ ký." });

        // verify signature
        if (!_payOs.VerifyWebhookSignature(req.data, req.signature))
            return Unauthorized(new { message = "Chữ ký không hợp lệ." });

        // Deserialize data
        var dataJson = JsonSerializer.Serialize(req.data);
        var data = JsonSerializer.Deserialize<PayOSWebhookData>(dataJson);
        if (data is null)
            return BadRequest(new { message = "Dữ liệu webhook không hợp lệ." });

        bool success = req.success ||
                       string.Equals(req.code, "00", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(data.code, "00", StringComparison.OrdinalIgnoreCase);

        // orderCode is long; map to Payment.OrderCode
        var orderCode = data.orderCode;

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderCode == orderCode, ct);

        if (payment == null)
        {
            _logger.LogWarning("Webhook PayOS: payment not found for orderCode {OrderCode}", orderCode);
            return NotFound(new { message = "Không tìm thấy payment.", orderCode });
        }

        // Amount match check
        if (payment.ExpectedAmount != data.amount)
        {
            _logger.LogWarning("Webhook PayOS: amount mismatch for payment {PaymentId}: expected {Expected} received {Received}", payment.Id, payment.ExpectedAmount, data.amount);
            return BadRequest(new { message = "Số tiền không khớp.", expected = payment.ExpectedAmount, received = data.amount });
        }

        // Idempotent: if already Paid, return OK
        if (payment.Status == PaymentStatus.Paid)
        {
            return Ok(new { message = "OK_ALREADY_PAID", paymentId = payment.Id, status = payment.Status.ToString() });
        }

        if (!success)
        {
            // mark failed
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Ok(new { message = "PAYMENT_FAILED", paymentId = payment.Id });
        }

        // Success: update payment and related rental
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;
            payment.LastEventAt = DateTime.UtcNow;

            // Update Rental
            if (payment.Type == PaymentType.Rental)
            {
                var rental = await _context.Rentals.FindAsync(new object?[] { payment.RefId }, ct);
                if (rental != null)
                {
                    // Only update if not already Active/PAID
                    if (rental.PaymentStatus != "PAID")
                    {
                        rental.PaymentStatus = "PAID";
                        rental.PaidAt = DateTime.UtcNow;
                        rental.ConfirmedAt = DateTime.UtcNow;

                        // Prefer to set Active (user đã thanh toán -> bắt đầu thuê)
                        rental.Status = RentalStatus.Active;

                        // Save transactionCode if present
                        rental.TransactionCode = data.reference ?? data.transactionCode ?? rental.TransactionCode;
                    }
                }
            }
            else if (payment.Type == PaymentType.Order)
            {
                var order = await _context.Orders.FindAsync(new object?[] { payment.RefId }, ct);
                if (order != null)
                {
                    if (order.PaymentStatus != "PAID")
                    {
                        order.PaymentStatus = "PAID";
                        order.PaidAt = DateTime.UtcNow;
                        order.TransactionCode = data.reference ?? data.transactionCode ?? order.TransactionCode;
                        order.Status = OrderStatus.Completed;
                    }
                }
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Ok(new
            {
                message = "PAYMENT_OK",
                paymentId = payment.Id,
                refId = payment.RefId,
                type = payment.Type.ToString()
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Lỗi khi xử lý webhook PayOS cho orderCode {OrderCode}", orderCode);
            return StatusCode(500, new { message = "Lỗi xử lý webhook.", error = ex.Message });
        }
    }

    /// <summary>
    /// 🔍 FE polling tình trạng thanh toán Rental
    /// </summary>
    [HttpGet("rental/status/{rentalId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRentalPaymentStatus(int rentalId, CancellationToken ct)
    {
        var rental = await _context.Rentals.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rentalId, ct);

        if (rental == null)
            return NotFound(new { message = "Không tìm thấy đơn thuê." });

        return Ok(new
        {
            rental.Id,
            rental.Status,
            paymentStatus = rental.PaymentStatus ?? "UNPAID",
            rental.PaymentUrl,
            rental.QrCodeUrl,
            rental.ShippingFee,
            rental.TotalPrice,
            rental.DiscountAmount,
            rental.PaidAt,
            rental.TransactionCode
        });
    }

    // Order - PayOS
    #region 
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
    #endregion
}

public class CreatePaymentLinkDto
{
    public int OrderId { get; set; }
}

public class CreateRentalPaymentLinkDto
{
    public int RentalId { get; set; }
}