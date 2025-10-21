using Backend.Data;
using Backend.DTOs;
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
        //[HttpPost("Checkout/{userId}")]
        //public async Task<IActionResult> Checkout(int userId, [FromBody] string shippingAddress)
        //{
        //    var cart = await _context.Carts
        //        .Include(c => c.Items)
        //        .ThenInclude(i => i.Product)
        //        .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut);

        //    if (cart == null || !cart.Items.Any())
        //        return BadRequest("Giỏ hàng trống hoặc không tồn tại.");

        //    // Tính tổng tiền
        //    var total = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        //    // Tạo đơn hàng
        //    var order = new Order
        //    {
        //        UserId = userId,
        //        TotalAmount = total,
        //        ShippingAddress = shippingAddress,
        //        Status = OrderStatus.Pending
        //    };

        //    // Copy các item từ cart sang order
        //    foreach (var item in cart.Items)
        //    {
        //        order.Items.Add(new OrderItem
        //        {
        //            ProductId = item.ProductId,
        //            Quantity = item.Quantity,
        //            UnitPrice = item.UnitPrice
        //        });
        //    }

        //    // Đánh dấu giỏ hàng đã checkout
        //    cart.IsCheckedOut = true;
        //    cart.UpdatedAt = DateTime.Now;

        //    // Lưu vào DB
        //    _context.Orders.Add(order);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { Message = "Đặt hàng thành công!", OrderId = order.Id });
        //}
        [HttpPost("Checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutOrderRequest req)
        {
            // Lấy cart mở
            var cart = await _context.Carts
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == req.UserId && !c.IsCheckedOut);

            if (cart == null || !cart.Items.Any())
                return BadRequest("Giỏ hàng trống hoặc không tồn tại.");

            // Tính tổng + validate tồn kho
            decimal total = 0m;
            foreach (var it in cart.Items)
            {
                if (it.Product == null)
                    return BadRequest($"Sản phẩm Id={it.ProductId} không tồn tại.");
                if (it.Product.Quantity < it.Quantity)
                    return BadRequest($"Sản phẩm '{it.Product.Name}' không đủ tồn.");
                total += it.UnitPrice * it.Quantity;
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserId = req.UserId,
                    TotalAmount = total,
                    ShippingAddress = req.ShippingAddress,
                    PaymentMethod = req.PaymentMethod,
                    // Trạng thái set bên dưới theo phương thức
                    Items = new List<OrderItem>()
                };

                // Copy items
                foreach (var it in cart.Items)
                {
                    order.Items.Add(new OrderItem
                    {
                        ProductId = it.ProductId,
                        Quantity = it.Quantity,
                        UnitPrice = it.UnitPrice
                    });
                }

                // Chính sách trạng thái theo PaymentMethod (không dùng PaymentStatus)
                if (req.PaymentMethod == PaymentMethod.COD)
                {
                    order.Status = OrderStatus.Pending;     // chờ giao/thu tiền
                }
                else // QR
                {
                    order.Status = OrderStatus.Processing;  // đã tạo đơn, chờ xác nhận thanh toán QR
                                                            // (tuỳ bạn: tạo mã tham chiếu/QR payload ở đây nếu cần)
                }

                // Trừ tồn kho ngay (giữ hàng). Nếu muốn “reserve” thay vì trừ, tách logic theo nhu cầu.
                foreach (var it in cart.Items)
                    it.Product!.Quantity -= it.Quantity;

                // Đánh dấu cart đã checkout
                cart.IsCheckedOut = true;
                cart.UpdatedAt = DateTime.UtcNow;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { Message = "Đặt hàng thành công!", OrderId = order.Id, PaymentMethod = order.PaymentMethod.ToString(), OrderStatus = order.Status.ToString() });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
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