﻿using System.Text;
using System.Text.Json;
using Backend.Models;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace Backend.Services
{
    public interface IPayOSService
    {
        bool VerifyWebhookSignature(IDictionary<string, object?> data, string signature);
        Task<PayOSCreatePaymentResult> CreatePaymentAsync(
        long orderCode, long amountVnd, string description, CancellationToken ct);

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
            var dict = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["amount"] = amountVnd.ToString(),          // số nguyên VND
                ["cancelUrl"] = _opt.CancelUrl,             // KHÔNG encode
                ["description"] = description ?? string.Empty,
                ["orderCode"] = orderCode.ToString(),
                ["returnUrl"] = _opt.ReturnUrl              // KHÔNG encode
            };

            var raw = BuildSignatureInput_NoEncode(dict);   // <-- dùng bản không encode
            var signature = HmacSha256(raw, _opt.ChecksumKey);

            var payload = new
            {
                orderCode,
                amount = amountVnd,
                description,
                returnUrl = _opt.ReturnUrl,
                cancelUrl = _opt.CancelUrl,
                signature
            };

            using var resp = await _http.PostAsync(
                "/v2/payment-requests",
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
                PaymentLinkId = data.TryGetProperty("paymentLinkId", out var pid) ? pid.GetString() : null
            };
        }


        // ---------- Verify webhook (bạn đang dùng) ----------
        public bool VerifyWebhookSignature(IDictionary<string, object?> data, string signature)
        {
            var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in data) sorted[kv.Key] = kv.Value?.ToString() ?? "";

            var raw = BuildSignatureInput_NoEncode(sorted);  // ❗ KHÔNG encode
            var calc = HmacSha256(raw, _opt.ChecksumKey);

            var ok = calc.Equals(signature, StringComparison.OrdinalIgnoreCase);
            // TODO: log raw/calc/got khi ok==false để soi lệch
            return ok;
        }
        private static string BuildSignatureInput(IDictionary<string, string> kvs)
        {
            var sb = new StringBuilder();
            foreach (var kv in kvs)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(kv.Key);
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(kv.Value));
            }
            return sb.ToString();
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

    }
}
