using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

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

    // POST: api/checkout/order
    [HttpPost("order")]
    public async Task<IActionResult> CheckoutOrder([FromBody] CheckoutOrderRequest req, CancellationToken ct)
    {
        try
        {
            var res = await _service.CheckoutOrderAsync(req, ct);
            return Ok(new { Message = res.Message, OrderId = res.OrderId });
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

    // POST: api/checkout/rental-by-days
    [HttpPost("rental-by-days")]
    public async Task<IActionResult> CheckoutRentalByDays([FromBody] CheckoutRentalByDaysRequest req, CancellationToken ct)
    {
        try
        {
            var res = await _service.CheckoutRentalByDaysAsync(req, ct);
            return Ok(new { Message = res.Message, RentalId = res.RentalId, res.RentalDays });
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

    // POST: api/checkout/rental-by-dates
    [HttpPost("rental-by-dates")]
    public async Task<IActionResult> CheckoutRentalByDates([FromBody] CheckoutRentalByDatesRequest req, CancellationToken ct)
    {
        try
        {
            var res = await _service.CheckoutRentalByDatesAsync(req, ct);
            return Ok(new { Message = res.Message, RentalId = res.RentalId, res.RentalDays });
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
}
