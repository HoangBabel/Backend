using Microsoft.AspNetCore.Mvc;
using Backend.Services;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipingFee : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShipingFee(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        /// <summary>
        /// API công khai để client kiểm tra phí ship trước khi đặt hàng
        /// </summary>
        [HttpGet("calculate")]
        public async Task<IActionResult> CalculateShipping(
            [FromQuery] int? from_district_id = null,
            [FromQuery] string? from_ward_code = null,
            [FromQuery] int to_district_id = 0,
            [FromQuery] string to_ward_code = "",
            [FromQuery] int service_id = 53321,
            [FromQuery] int weight = 200,
            [FromQuery] int length = 20,
            [FromQuery] int width = 20,
            [FromQuery] int height = 20
        )
        {
            try
            {
                var request = new ShippingFeeRequest
                {
                    FromDistrictId = from_district_id,
                    FromWardCode = from_ward_code,
                    ToDistrictId = to_district_id,
                    ToWardCode = to_ward_code,
                    ServiceId = service_id,
                    Weight = weight,
                    Length = length,
                    Width = width,
                    Height = height
                };

                var result = await _shippingService.CalculateShippingFeeAsync(request);

                if (!result.Success)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                return Ok(new
                {
                    shipping_fee = result.ShippingFee,
                    service_type = result.ServiceType,
                    service_id = result.ServiceId,
                    from_district_id = result.FromDistrictId,
                    from_ward_code = result.FromWardCode,
                    to_district_id = result.ToDistrictId,
                    to_ward_code = result.ToWardCode,
                    weight = result.Weight,
                    length,
                    width,
                    height
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// API lấy thông tin địa chỉ kho/shop mặc định
        /// </summary>
        [HttpGet("default-location")]
        public IActionResult GetDefaultLocation()
        {
            return Ok(new
            {
                from_district_id = ShippingService.FROM_DISTRICT_ID,
                from_ward_code = ShippingService.FROM_WARD_CODE,
                description = "Kho/Shop mặc định"
            });
        }
    }
}
