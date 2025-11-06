// Services/ShippingService.cs
using System.Text.Json;
using System.Text;

namespace Backend.Services
{
    public interface IShippingService
    {
        Task<ShippingFeeResult> CalculateShippingFeeAsync(ShippingFeeRequest request);
    }

    public class ShippingService : IShippingService
    {
        private readonly HttpClient _httpClient;
        private const string TOKEN = "065d6eef-b594-11f0-adb5-b2a18a23deba";
        private const string SHOP_ID = "6089908";
        private const string GHN_BASE_URL = "https://online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/fee";

        // Địa chỉ kho/shop (from address) - CẤU HÌNH CỐ ĐỊNH
        public const int FROM_DISTRICT_ID = 1442; // Quận Ba Đình, Hà Nội
        public const string FROM_WARD_CODE = "21012"; // Phường Phúc Xá

        public ShippingService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Token", TOKEN);
            _httpClient.DefaultRequestHeaders.Add("ShopId", SHOP_ID);
        }

        public async Task<ShippingFeeResult> CalculateShippingFeeAsync(ShippingFeeRequest request)
        {
            try
            {
                // Validate
                if (request.ToDistrictId <= 0)
                    return ShippingFeeResult.Fail("District ID must be greater than 0");

                if (string.IsNullOrEmpty(request.ToWardCode))
                    return ShippingFeeResult.Fail("Ward code is required");

                if (request.Weight <= 0)
                    return ShippingFeeResult.Fail("Weight must be greater than 0");

                if (!new[] { 53320, 53321, 53322 }.Contains(request.ServiceId))
                    return ShippingFeeResult.Fail("Invalid service_id");

                // Tạo request body
                var requestBody = new
                {
                    from_district_id = request.FromDistrictId ?? FROM_DISTRICT_ID,
                    from_ward_code = request.FromWardCode ?? FROM_WARD_CODE,
                    to_district_id = request.ToDistrictId,
                    to_ward_code = request.ToWardCode,
                    service_id = request.ServiceId,
                    weight = request.Weight,
                    length = request.Length,
                    width = request.Width,
                    height = request.Height,
                    insurance_value = request.InsuranceValue
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GHN_BASE_URL, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ShippingFeeResult.Fail($"GHN API Error: {content}");
                }

                using (JsonDocument document = JsonDocument.Parse(content))
                {
                    var root = document.RootElement;

                    if (root.TryGetProperty("code", out JsonElement codeElement) && codeElement.GetInt32() == 200)
                    {
                        if (root.TryGetProperty("data", out JsonElement dataElement) &&
                            dataElement.TryGetProperty("total", out JsonElement totalElement))
                        {
                            var shippingFee = totalElement.GetInt32();

                            return new ShippingFeeResult
                            {
                                Success = true,
                                ShippingFee = shippingFee,
                                ServiceId = request.ServiceId,
                                ServiceType = GetServiceTypeName(request.ServiceId),
                                FromDistrictId = request.FromDistrictId ?? FROM_DISTRICT_ID,
                                FromWardCode = request.FromWardCode ?? FROM_WARD_CODE,
                                ToDistrictId = request.ToDistrictId,
                                ToWardCode = request.ToWardCode,
                                Weight = request.Weight
                            };
                        }
                    }
                }

                return ShippingFeeResult.Fail("Cannot calculate shipping fee");
            }
            catch (Exception ex)
            {
                return ShippingFeeResult.Fail(ex.Message);
            }
        }

        private string GetServiceTypeName(int serviceId)
        {
            return serviceId switch
            {
                53320 => "GHN Express",
                53321 => "GHN Tiết Kiệm",
                53322 => "GHN Chuẩn",
                _ => "Unknown"
            };
        }
    }

    // DTO Request
    public class ShippingFeeRequest
    {
        public int? FromDistrictId { get; set; } // Nullable để dùng mặc định
        public string? FromWardCode { get; set; }
        public int ToDistrictId { get; set; }
        public string ToWardCode { get; set; } = string.Empty;
        public int ServiceId { get; set; } = 53321;
        public int Weight { get; set; }
        public int Length { get; set; } = 20;
        public int Width { get; set; } = 20;
        public int Height { get; set; } = 20;
        public int InsuranceValue { get; set; } = 0;
    }

    // DTO Result
    public class ShippingFeeResult
    {
        public bool Success { get; set; }
        public decimal ShippingFee { get; set; }
        public int ServiceId { get; set; }
        public string? ServiceType { get; set; }
        public int FromDistrictId { get; set; }
        public string? FromWardCode { get; set; }
        public int ToDistrictId { get; set; }
        public string? ToWardCode { get; set; }
        public int Weight { get; set; }
        public string? ErrorMessage { get; set; }

        public static ShippingFeeResult Fail(string message)
        {
            return new ShippingFeeResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }
}
