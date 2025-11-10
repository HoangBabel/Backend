namespace Backend.Models
{
    public sealed class PayOSWebhookEnvelope
    {
        public string code { get; set; } = "";          // "00" = success
        public string desc { get; set; } = "";
        public bool success { get; set; }
        public System.Text.Json.JsonElement data { get; set; }   // <-- thay Dictionary
        public string signature { get; set; } = "";     // chữ ký của PayOS (HMAC)
    }

    // Dữ liệu cụ thể bên trong "data"
    public sealed class PayOSWebhookData
    {
        public long orderCode { get; set; }     // dùng làm orderId
        public long amount { get; set; }        // số tiền VND
        public string? code { get; set; }
        public string? description { get; set; }
        public string? reference { get; set; }  // mã tham chiếu NH (nếu có)
        public string? transactionDateTime { get; set; }
        public string? paymentLinkId { get; set; }
    }

    public sealed class PayOSOptions
    {
        public string BaseUrl { get; set; } = null!;
        public string ClientId { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ChecksumKey { get; set; } = null!;
        public string ReturnUrl { get; set; } = null!;
        public string CancelUrl { get; set; } = null!;
    }

    public sealed class PayOSCreatePaymentResult
    {
        public string CheckoutUrl { get; set; } = null!;
        public string? QrCode { get; set; } // có thể null
        public string? PaymentLinkId { get; set; }
        public long OrderCodeUsed { get; set; }
    }
}
