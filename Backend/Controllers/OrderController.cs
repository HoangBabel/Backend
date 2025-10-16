using Backend.Data;
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

        // POST: api/Order/Checkout/5
        [HttpPost("Checkout/{userId}")]
        public async Task<IActionResult> Checkout(int userId, [FromBody] string shippingAddress)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut);

            if (cart == null || !cart.Items.Any())
                return BadRequest("Giỏ hàng trống hoặc không tồn tại.");

            // Tính tổng tiền
            var total = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = userId,
                TotalAmount = total,
                PaymentMethod = "COD",
                Status = OrderStatus.Pending
            };

            // Copy các item từ cart sang order
            foreach (var item in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            // Đánh dấu giỏ hàng đã checkout
            cart.IsCheckedOut = true;
            cart.UpdatedAt = DateTime.Now;

            // Lưu vào DB
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đặt hàng thành công!", OrderId = order.Id });
        }

        // GET: api/Order/User/5
        [HttpGet("User/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
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