using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Backend.DTOs.DailyRentalDtos;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DailyRentalsController : Controller
    {
        private readonly IDailyRentalService _svc;
        private readonly AppDbContext _db;

        public DailyRentalsController(IDailyRentalService svc, AppDbContext db)
        {
            _svc = svc;
            _db = db;
        }

        public static int GetUserId(ClaimsPrincipal user)
        {
            // tuỳ bạn map: "sub" / ClaimTypes.NameIdentifier / "uid"
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirstValue("sub")
                     ?? user.FindFirstValue("uid");
            if (string.IsNullOrEmpty(id)) throw new UnauthorizedAccessException("Token không chứa user id.");
            return int.Parse(id);
        }

        // POST: /api/DailyRentals/quote
        [HttpPost("quote")]
        public async Task<ActionResult<QuoteDailyResponseDto>> Quote([FromBody] QuoteDailyRequestDto dto)
        {
            try
            {
                var res = await _svc.GetDailyQuoteAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: /api/DailyRentals
        [HttpPost]
        public async Task<ActionResult<CreateDailyRentalResponseDto>> Create([FromBody] CreateDailyRentalRequestDto dto)
        {
            try
            {
                var userId = GetUserId(User);
                var res = await _svc.CreateDailyRentalAsync(dto, userId); // ✅ giữ nguyên quantity
                return CreatedAtAction(nameof(GetById), new { id = res.RentalId }, res);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // GET: /api/DailyRentals/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Rental>> GetById(int id)
        {
            var rental = await _db.Rentals
                .Include(r => r.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            return rental is null ? NotFound() : Ok(rental);
        }

        // GET: /api/DailyRentals/{userid}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserRentals(int userId)
        {
            var rentals = await _db.Rentals
                .Include(r => r.Items)
                .ThenInclude(i => i.Product)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return Ok(rentals);
        }

        // POST: /api/DailyRentals/{id}/activate
        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult> Activate(int id)
        {
            var rental = await _db.Rentals
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental is null) return NotFound();
            try
            {
                rental.Activate();
                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // --- quyết toán / hoàn cọc ---
        public record SettleRentalRequestDto(DateTime ReturnedAt, decimal CleaningFee, decimal DamageFee);

        // POST: /api/DailyRentals/{id}/settle
        [HttpPost("{id:int}/settle")]
        public async Task<ActionResult<object>> Settle(int id, [FromBody] SettleRentalRequestDto dto)
        {
            var rental = await _db.Rentals
                .Include(r => r.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental is null) return NotFound();

            try
            {
                // Tính phí trễ theo helper bạn đã có
                var lateFee = Backend.Helpers.RentalSettlementHelper.ComputeTotalLateFee(rental, dto.ReturnedAt);

                rental.SetSettlement(
                    returnedAt: dto.ReturnedAt,
                    lateFee: lateFee,
                    cleaningFee: dto.CleaningFee,
                    damageFee: dto.DamageFee
                );

                await _db.SaveChangesAsync();

                var lateDays = Backend.Helpers.RentalSettlementHelper.ComputeLateDays(rental.EndDate, dto.ReturnedAt);

                return Ok(new
                {
                    rental.Id,
                    LateDays = lateDays,
                    rental.LateFee,
                    rental.CleaningFee,
                    rental.DamageFee,
                    rental.DepositPaid,
                    rental.DepositRefund,
                    rental.TotalPrice,
                    NetCollectOrRefund = -rental.DepositRefund // âm nghĩa là trả lại KH
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
