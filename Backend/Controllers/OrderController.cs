using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }



        // GET: api/Order/User/5
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            return Ok(orders);
        }

        // PUT: api/Order/Payment/5
        [HttpPut("Payment/{orderId}")]
        public async Task<IActionResult> MarkAsPaid(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            order.Status = OrderStatus.Completed;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Thanh toán thành công!" });
        }
    }
}