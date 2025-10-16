using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RentalController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RentalController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRental([FromBody] Rentals rental)
        {
            // Tính tổng tiền thuê
            foreach (var item in rental.Items)
            {
                item.SubTotal = item.PricePerDay * item.RentalDays;
            }
            rental.TotalPrice = rental.Items.Sum(i => i.SubTotal);
            rental.Status = RentalStatus.Pending;
            rental.StartDate = DateTime.Now;
            rental.EndDate = rental.StartDate.AddDays(rental.Items.Max(i => i.RentalDays));

            _context.Rentals.Add(rental);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo đơn thuê thành công", rental });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserRentals(int userId)
        {
            var rentals = await _context.Rentals
                .Include(r => r.Items)
                .ThenInclude(i => i.Product)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return Ok(rentals);
        }

        [HttpPut("complete/{id}")]
        public async Task<IActionResult> CompleteRental(int id)
        {
            var rental = await _context.Rentals.FindAsync(id);
            if (rental == null) return NotFound();

            rental.Status = RentalStatus.Completed;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đơn thuê đã hoàn tất" });
        }
    }
}
