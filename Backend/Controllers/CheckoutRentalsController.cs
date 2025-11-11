using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RentalCheckoutController : ControllerBase
    {
        private readonly IRentalCheckoutService _service;
        private readonly IPayOSService _payOs;

        public RentalCheckoutController(IRentalCheckoutService service, IPayOSService payOs)
        {
            _service = service;
            _payOs = payOs;
        }

        [HttpPost]
        public async Task<IActionResult> CheckoutRental(
      [FromBody] CheckoutRentalRequest? req,
      [FromQuery] int? devUserId,
      CancellationToken ct)
        {
            if (req == null)
                return BadRequest("Body JSON rỗng hoặc sai Content-Type: application/json.");

            int userId;
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out var uid))
                userId = uid;
            else if (devUserId.HasValue)
                userId = devUserId.Value;
            else
                return Unauthorized("Thiếu token hoặc devUserId (?devUserId=1) để test.");

            try
            {
                var res = await _service.CheckoutRentalAsync(userId, req, ct);
                return Ok(new
                {
                    res.Message,
                    res.RentalId,
                    res.Subtotal,
                    res.ShippingFee,
                    res.Discount,
                    res.Deposit,
                    res.VoucherCode,
                    res.ServiceType,
                    res.Weight,
                    res.FinalAmount,
                    PaymentMethod = res.PaymentMethod.ToString(),
                    res.CheckoutUrl,
                    res.QrCode,
                    res.PaymentLinkId
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi checkout đơn thuê.", detail = ex.Message });
            }
        }
    }
}
