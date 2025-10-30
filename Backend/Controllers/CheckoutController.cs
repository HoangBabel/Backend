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

    // ==============================
    // 🧾 1️⃣ CHECKOUT ĐƠN HÀNG
    // ==============================
    //[HttpPost("order")]
    //public async Task<IActionResult> CheckoutOrder([FromBody] CheckoutOrderRequest req, CancellationToken ct)
    //{
    //    try
    //    {
    //        // 🔹 Lấy userId từ token
    //        var userIdClaim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
    //        if (userIdClaim == null)
    //            return Unauthorized("Không xác định được người dùng.");

    //        if (!int.TryParse(userIdClaim.Value, out var userId))
    //            return Unauthorized("Token không hợp lệ.");

    //        // 🔹 Gọi sang service (truyền userId riêng, không lấy từ req)
    //        var res = await _service.CheckoutOrderAsync(userId, req, ct);

    //        // 🔹 Tạo link thanh toán (nếu cần)
    //        string? momoUrl = null;
    //        if (res.PaymentMethod == PaymentMethod.QR && res.FinalAmount > 0m)
    //        {
    //            var amountRounded = decimal.Round(res.FinalAmount, 0, MidpointRounding.AwayFromZero);
    //            momoUrl = await CreateMomoPaymentUrlAsync(res.OrderId, amountRounded);
    //        }

    //        return Ok(new
    //        {
    //            res.Message,
    //            res.OrderId,
    //            Subtotal = res.Subtotal,
    //            Discount = res.Discount,
    //            FinalAmount = res.FinalAmount,
    //            PaymentMethod = res.PaymentMethod.ToString(),
    //            Voucher = res.VoucherCode,
    //            PayUrl = momoUrl
    //        });
    //    }
    //    catch (InvalidOperationException ex)
    //    {
    //        return BadRequest(ex.Message);
    //    }
    //    catch
    //    {
    //        return StatusCode(500, "Có lỗi khi checkout đơn hàng.");
    //    }
    //}
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //[HttpPost("order")]
    //public async Task<IActionResult> CheckoutOrder([FromBody] CheckoutOrderRequest req, CancellationToken ct)
    //{
    //    try
    //    {
    //        // Lấy userId từ token như bạn đã làm...
    //        var userIdClaim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
    //        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
    //            return Unauthorized("Token không hợp lệ.");

    //        var res = await _service.CheckoutOrderAsync(userId, req, ct); // tạo Order, tính FinalAmount, gắn voucher...

    //        string? checkoutUrl = null;
    //        string? qrCode = null;

    //        if (res.PaymentMethod == PaymentMethod.QR && res.FinalAmount > 0m)
    //        {
    //            var amountVnd = (long)decimal.Round(res.FinalAmount, 0, MidpointRounding.AwayFromZero);
    //            var desc = $"ORDER-{res.OrderId}";
    //            var pay = await _payOs.CreatePaymentAsync(res.OrderId, amountVnd, desc, ct);
    //            checkoutUrl = pay.CheckoutUrl;
    //            qrCode = pay.QrCode;
    //        }

    //        return Ok(new
    //        {
    //            res.Message,
    //            res.OrderId,
    //            Subtotal = res.Subtotal,
    //            Discount = res.Discount,
    //            FinalAmount = res.FinalAmount,
    //            PaymentMethod = res.PaymentMethod.ToString(),
    //            Voucher = res.VoucherCode,
    //            CheckoutUrl = checkoutUrl,  // FE mở link này
    //            QrCode = qrCode        // hoặc hiển thị ảnh QR nếu có
    //        });
    //    }
    //    catch (InvalidOperationException ex)
    //    {
    //        return BadRequest(ex.Message);
    //    }
    //    catch
    //    {
    //        return StatusCode(500, "Có lỗi khi checkout đơn hàng.");
    //    }
    //}
    [HttpPost("order")]
    public async Task<IActionResult> CheckoutOrder(
    [FromBody] CheckoutOrderRequest? req,
    [FromQuery] int? devUserId,
    CancellationToken ct)
    {
        // 1) Kiểm tra body
        if (req is null)
            return BadRequest("Body JSON rỗng hoặc sai Content-Type: application/json.");

        // 2) Lấy userId: ưu tiên từ token, nếu không có dùng devUserId (chỉ để test)
        int userId;
        var claim = User.FindFirst("id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var uid)) userId = uid;
        else if (devUserId.HasValue) userId = devUserId.Value;
        else return Unauthorized("Thiếu token hoặc devUserId (?devUserId=1) để test.");

        // 3) Gọi service như bình thường
        var res = await _service.CheckoutOrderAsync(userId, req, ct);

        string? checkoutUrl = null;
        string? qrCode = null;
        if (res.PaymentMethod == PaymentMethod.QR && res.FinalAmount > 0m)
        {
            try
            {
                var amountVnd = (long)decimal.Round(res.FinalAmount, 0, MidpointRounding.AwayFromZero);
                var pay = await _payOs.CreatePaymentAsync(res.OrderId, amountVnd, $"ORDER-{res.OrderId}", ct);
                checkoutUrl = pay.CheckoutUrl;
                qrCode = pay.QrCode;
            }
            catch (Exception ex)
            {
                // Trả về để FE biết hiển thị thông báo, vẫn có OrderId cho user thử lại
                return Ok(new
                {
                    res.Message,
                    res.OrderId,
                    Subtotal = res.Subtotal,
                    Discount = res.Discount,
                    FinalAmount = res.FinalAmount,
                    PaymentMethod = res.PaymentMethod.ToString(),
                    Voucher = res.VoucherCode,
                    Error = "Không tạo được liên kết thanh toán PayOS",
                    Detail = ex.Message
                });
            }
        }

        return Ok(new
        {
            res.Message,
            res.OrderId,
            Subtotal = res.Subtotal,
            Discount = res.Discount,
            FinalAmount = res.FinalAmount,
            PaymentMethod = res.PaymentMethod.ToString(),
            Voucher = res.VoucherCode,
            CheckoutUrl = checkoutUrl,
            QrCode = qrCode
        });
    }


    // ==============================
    // 🏠 2️⃣ THUÊ THEO SỐ NGÀY
    // ==============================
    [HttpPost("rental-by-days")]
    public async Task<IActionResult> CheckoutRentalByDays([FromBody] CheckoutRentalByDaysRequest req, CancellationToken ct)
    {
        try
        {
            var res = await _service.CheckoutRentalByDaysAsync(req, ct);

            // ✅ Tạo link thanh toán MoMo cho đơn thuê
            var momoUrl = await CreateMomoPaymentUrlAsync(res.RentalId, req.TotalAmount);

            return Ok(new
            {
                Message = res.Message,
                RentalId = res.RentalId,
                res.RentalDays,
                PayUrl = momoUrl
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch
        {
            return StatusCode(500, "Có lỗi khi tạo đơn thuê.");
        }
    }

    // ==============================
    // 📅 3️⃣ THUÊ THEO KHOẢNG NGÀY
    // ==============================
    [HttpPost("rental-by-dates")]
    public async Task<IActionResult> CheckoutRentalByDates([FromBody] CheckoutRentalByDatesRequest req, CancellationToken ct)
    {
        try
        {
            var res = await _service.CheckoutRentalByDatesAsync(req, ct);

            // ✅ Tạo link thanh toán MoMo cho đơn thuê
            var momoUrl = await CreateMomoPaymentUrlAsync(res.RentalId, req.TotalAmount);

            return Ok(new
            {
                Message = res.Message,
                RentalId = res.RentalId,
                res.RentalDays,
                PayUrl = momoUrl
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch
        {
            return StatusCode(500, "Có lỗi khi tạo đơn thuê.");
        }
    }

    // ==============================
    // 💳 4️⃣ THANH TOÁN MOMO (API CHÍNH)
    // ==============================

    private const string endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
    private const string partnerCode = "MOMOBKUN20180529";
    private const string accessKey = "klm05TvNBzhg7h7j";
    private const string secretKey = "at67qH6mk8w5Y1nAyMoYKMWACiEi2bsa";


    private async Task<string> CreateMomoPaymentUrlAsync(int orderId, decimal amount)
    {
        string requestId = Guid.NewGuid().ToString();
        string orderInfo = $"Thanh toán đơn #{orderId}";
        string redirectUrl = "https://yourdomain.com/payment-success";
        string ipnUrl = "https://yourdomain.com/api/checkout/momo-ipn";

        // 🔹 Chuỗi ký
        string rawHash = $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={ipnUrl}" +
                         $"&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}" +
                         $"&redirectUrl={redirectUrl}&requestId={requestId}&requestType=captureWallet";

        string signature = CreateSignature(secretKey, rawHash);

        // 🔹 Tạo payload gửi đến MoMo
        var payload = new
        {
            partnerCode,
            requestId,
            amount = amount.ToString(),
            orderId = orderId.ToString(),
            orderInfo,
            redirectUrl,
            ipnUrl,
            extraData = "",
            requestType = "captureWallet",
            signature,
            lang = "vi"
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // 🔹 Trích xuất payUrl từ JSON trả về
        dynamic result = JsonConvert.DeserializeObject(responseContent)!;
        return result.payUrl ?? "";
    }

    private static string CreateSignature(string secretKey, string rawHash)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        byte[] hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawHash));
        return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
    }

    // ==============================
    // 📥 5️⃣ NHẬN KẾT QUẢ THANH TOÁN MOMO (CALLBACK)
    // ==============================
    [HttpPost("momo-ipn")]
    public IActionResult MomoIpn([FromBody] dynamic data)
    {
        string orderId = data.orderId;
        string resultCode = data.resultCode;

        if (resultCode == "0")
        {
            // ✅ Thanh toán thành công → Cập nhật trạng thái trong DB
            // _service.UpdatePaymentStatus(orderId, true);
            return Ok(new { message = "Payment success" });
        }
        else
        {
            return BadRequest(new { message = "Payment failed or canceled" });
        }
    }
}
