using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Backend.Models;
using Microsoft.Extensions.Options;

namespace Backend.Services
{
    public interface IPayOSService
    {
        bool VerifyWebhookSignature(string rawDataJson, string signature);

        // Trả về PayOSCreatePaymentResult (trong đó có OrderCodeUsed)
        Task<PayOSCreatePaymentResult> CreatePaymentAsync(
            long orderCode, long amountVnd, string description, CancellationToken ct);

        Task<PayOSCreatePaymentResult> CreatePaymentWithNewCodeAsync(
            long amountVnd, string description, CancellationToken ct);

    }
    public sealed class PayOSService : IPayOSService
    {

        private readonly HttpClient _http;
        private readonly PayOSOptions _opt;

        public PayOSService(HttpClient http, IOptions<PayOSOptions> opt)
        {
            _http = http;
            _opt = opt.Value;

            _http.BaseAddress ??= new Uri(_opt.BaseUrl);
            _http.DefaultRequestHeaders.Remove("x-client-id");
            _http.DefaultRequestHeaders.Remove("x-api-key");
            _http.DefaultRequestHeaders.Add("x-client-id", _opt.ClientId);
            _http.DefaultRequestHeaders.Add("x-api-key", _opt.ApiKey);
        }

        // ---------- Tạo link/QR ----------
        public async Task<PayOSCreatePaymentResult> CreatePaymentAsync(
     long orderCode, long amountVnd, string description, CancellationToken ct)
        {
            // hàm con thực thi 1 lần với orderCode cụ thể
            async Task<PayOSCreatePaymentResult> CallAsync(long oc)
            {
                var dict = new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["amount"] = amountVnd.ToString(CultureInfo.InvariantCulture), // số dạng invariant
                    ["cancelUrl"] = _opt.CancelUrl,
                    ["description"] = description ?? string.Empty,
                    ["orderCode"] = oc.ToString(CultureInfo.InvariantCulture),
                    ["returnUrl"] = _opt.ReturnUrl
                };

                var raw = BuildSignatureInput_NoEncode(dict);
                var signature = HmacSha256(raw, _opt.ChecksumKey);

                var payload = new
                {
                    orderCode = oc,
                    amount = amountVnd,
                    description,
                    returnUrl = _opt.ReturnUrl,
                    cancelUrl = _opt.CancelUrl,
                    signature
                };

                using var resp = await _http.PostAsync("/v2/payment-requests",
             new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
             ct);

                var txt = await resp.Content.ReadAsStringAsync(ct);
                resp.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(txt);
                var root = doc.RootElement;
                var bodyCode = root.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";
                var bodyDesc = root.TryGetProperty("desc", out var d) ? d.GetString() ?? "" : "";
                if (!string.Equals(bodyCode, "00", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"PayOS failed: code={bodyCode}, desc={bodyDesc}, body={txt}");

                var data = root.GetProperty("data");
                var checkoutUrl = data.TryGetProperty("checkoutUrl", out var cu) ? cu.GetString() : null;
                if (string.IsNullOrEmpty(checkoutUrl))
                    throw new InvalidOperationException($"PayOS data.checkoutUrl is empty. body={txt}");

                return new PayOSCreatePaymentResult
                {
                    CheckoutUrl = checkoutUrl!,
                    QrCode = data.TryGetProperty("qrCode", out var qr) ? qr.GetString() : null,
                    PaymentLinkId = data.TryGetProperty("paymentLinkId", out var pid) ? pid.GetString() : null,
                    OrderCodeUsed = oc                                 // ✅ gán mã thực dùng
                };
            }

            try
            {
                return await CallAsync(orderCode);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("code=231"))
            {
                // bị trùng -> sinh mã mới và thử lại 1 lần
                var oc2 = NewOrderCode();
                return await CallAsync(oc2);
            }
        }


        // ---------- Verify webhook (bạn đang dùng) ----------
        public bool VerifyWebhookSignature(string rawDataJson, string signature)
        {
            using var doc = JsonDocument.Parse(rawDataJson);
            var dict = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                var v = p.Value;
                dict[p.Name] = v.ValueKind switch
                {
                    JsonValueKind.String => v.GetString() ?? "",
                    JsonValueKind.Number => v.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "",
                    _ => v.GetRawText()
                };
            }
            var raw = BuildSignatureInput_NoEncode(dict);
            var calc = HmacSha256(raw, _opt.ChecksumKey);
            return calc.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<PayOSCreatePaymentResult> CreatePaymentWithNewCodeAsync(
        long amountVnd, string description, CancellationToken ct)
        {
            var oc = NewOrderCode();
            return await CreatePaymentAsync(oc, amountVnd, description, ct);
        }


        private static string BuildSignatureInput_NoEncode(IDictionary<string, string> kvs)
        {
            // key sort alpha, value giữ NGUYÊN (không Uri.EscapeDataString)
            var sb = new StringBuilder();
            foreach (var kv in kvs)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(kv.Key);
                sb.Append('=');
                sb.Append(kv.Value ?? string.Empty);
            }
            return sb.ToString();
        }

        private static string HmacSha256(string raw, string key)
        {
            using var h = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = h.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static long NewOrderCode()
        {
            // millis + 3 số ngẫu nhiên để tránh trùng trong cùng mili-giây
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var rnd = Random.Shared.Next(100, 999);
            return ts * 1000 + rnd;
        }
    }
}
