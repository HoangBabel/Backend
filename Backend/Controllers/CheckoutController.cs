using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _service;

    public CheckoutController(ICheckoutService service)
    {
        _service = service;
    }

    // ==============================
    // 🧾 1️⃣ CHECKOUT ĐƠN HÀNG
    // ==============================
    [HttpPost("order")]
    public async Task<IActionResult> CheckoutOrder([FromBody] CheckoutOrderRequest req, CancellationToken ct)
    {
        try
        {
            var res = await _service.CheckoutOrderAsync(req, ct);

            // ✅ Gọi API MoMo để tạo thanh toán sau khi checkout thành công
            var momoUrl = await CreateMomoPaymentUrlAsync(res.OrderId, req.TotalAmount);

            return Ok(new
            {
                Message = res.Message,
                OrderId = res.OrderId,
                PayUrl = momoUrl // URL chuyển đến MoMo hoặc hiển thị QR
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch
        {
            return StatusCode(500, "Có lỗi khi checkout đơn hàng.");
        }
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
