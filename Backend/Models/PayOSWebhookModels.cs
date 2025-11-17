namespace Backend.Models
{
    // Gói dữ liệu webhook PayOS gửi về
    public sealed class PayOSWebhookEnvelope
    {
        public string code { get; set; } = "";               // "00" = success
        public string desc { get; set; } = "";
        public bool success { get; set; }
        public Dictionary<string, object?> data { get; set; } = new();  // chứa orderCode, amount, ...
        public string signature { get; set; } = "";          // chữ ký HMAC SHA256 của PayOS
    }

    // Dữ liệu chi tiết bên trong "data"
    public sealed class PayOSWebhookData
    {
        public long orderCode { get; set; }                  // Mã đơn hàng (dùng làm Order.Id)
        public long amount { get; set; }                     // Số tiền thanh toán (VND)
        public string? code { get; set; }                    // Mã trạng thái trả về, ví dụ "00"
        public string? description { get; set; }             // Mô tả giao dịch
        public string? reference { get; set; }               // Mã tham chiếu ngân hàng
        public string? transactionDateTime { get; set; }     // Thời điểm thanh toán (ISO8601)
        public string? paymentLinkId { get; set; }           // ID liên kết thanh toán PayOS
        public string? transactionCode { get; set; }         // Mã giao dịch (thường khác orderCode)
    }

    // Cấu hình PayOS (bind từ appsettings.json: "PayOS")
    public sealed class PayOSOptions
    {
        public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
        public string ClientId { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ChecksumKey { get; set; } = null!;
        public string ReturnUrl { get; set; } = null!;
        public string CancelUrl { get; set; } = null!;
    }

    // Kết quả tạo liên kết thanh toán
    public sealed class PayOSCreatePaymentResult
    {
        public string CheckoutUrl { get; set; } = null!;     // URL thanh toán cho người dùng
        public string? QrCode { get; set; }                  // URL QR code (tuỳ chọn)
        public string? PaymentLinkId { get; set; }           // Mã liên kết thanh toán (PayOS)
        public string? TransactionCode { get; set; }         // Mã giao dịch (nếu PayOS trả về)
    }
}
