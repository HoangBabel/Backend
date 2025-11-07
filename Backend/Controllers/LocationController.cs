using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private const string TOKEN = "065d6eef-b594-11f0-adb5-b2a18a23deba"; // Thay your-token-here bằng token thật
        private const string GHN_BASE_URL = "https://online-gateway.ghn.vn/shiip/public-api/master-data";

        public LocationController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Token", TOKEN);
        }

        [HttpGet("province")]
        public async Task<IActionResult> GetProvince()
        {
            try
            {
                string apiUrl = $"{GHN_BASE_URL}/province";

                var response = await _httpClient.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();

                return Ok(content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts(int? province_id = null)
        {
            try
            {
                string apiUrl = $"{GHN_BASE_URL}/district";
                if (province_id.HasValue)
                {
                    apiUrl += $"?province_id={province_id.Value}";
                }

                var response = await _httpClient.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();

                return Ok(content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("wards")]
        public async Task<IActionResult> GetWards(int district_id)
        {
            try
            {
                if (district_id <= 0)
                {
                    return BadRequest(new { error = "district_id is required and must be greater than 0" });
                }

                string apiUrl = $"{GHN_BASE_URL}/ward?district_id={district_id}";
                var response = await _httpClient.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();

                return Ok(content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }




}