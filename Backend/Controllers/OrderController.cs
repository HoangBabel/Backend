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
        //[HttpPost("Checkout")]
        //public async Task<IActionResult> Checkout([FromBody] CheckoutOrderRequest req, CancellationToken ct)
        //{
        //    // 1) Lấy userId từ token
        //    var userIdClaim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        //    if (userIdClaim == null) return Unauthorized("Không xác định được người dùng.");
        //    if (!int.TryParse(userIdClaim.Value, out var userId))
        //        return Unauthorized("Token không hợp lệ.");

        //    // 2) Lấy cart mở
        //    var cart = await _context.Carts
        //        .Include(c => c.Items).ThenInclude(i => i.Product)
        //        .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsCheckedOut, ct);

        //    if (cart == null || !cart.Items.Any())
        //        return BadRequest("Giỏ hàng trống hoặc không tồn tại.");

        //    // 3) Tính tổng + validate tồn kho
        //    decimal subtotal = 0m;
        //    foreach (var it in cart.Items)
        //    {
        //        if (it.Product == null)
        //            return BadRequest($"Sản phẩm Id={it.ProductId} không tồn tại.");
        //        if (it.Product.Quantity < it.Quantity)
        //            return BadRequest($"Sản phẩm '{it.Product.Name}' không đủ tồn.");
        //        subtotal += it.UnitPrice * it.Quantity;
        //    }

        //    // 4) Xử lý voucher (nếu có) – KHÔNG dùng shipping fee
        //    Vouncher? voucher = null;
        //    decimal discount = 0m;

        //    if (!string.IsNullOrWhiteSpace(req.VoucherCode))
        //    {
        //        var code = req.VoucherCode.Trim();
        //        voucher = await _context.Vounchers.FirstOrDefaultAsync(v => v.Code == code, ct);
        //        if (voucher == null)
        //            return BadRequest("Mã voucher không tồn tại.");

        //        if (!VoucherValidator(voucher, subtotal))
        //            return BadRequest("Voucher không còn hiệu lực hoặc không đạt điều kiện.");

        //        // Nếu bạn giữ hàm 3 tham số thì truyền 0m: VoucherCalculator.CalcDiscount(voucher, subtotal, 0m)
        //        discount = VoucherCalculator.CalcDiscount(voucher, subtotal);
        //    }

        //    // 5) Final amount
        //    var finalAmount = subtotal - discount;
        //    if (finalAmount < 0) finalAmount = 0;

        //    await using var tx = await _context.Database.BeginTransactionAsync(ct);
        //    try
        //    {
        //        // 6) Tạo Order + gắn voucher
        //        var order = new Order
        //        {
        //            UserId = userId,
        //            OrderDate = DateTime.UtcNow,
        //            ShippingAddress = req.ShippingAddress,
        //            PaymentMethod = req.PaymentMethod,
        //            // Trạng thái set bên dưới
        //            TotalAmount = subtotal,
        //            DiscountAmount = discount,
        //            FinalAmount = finalAmount,
        //            VoucherId = voucher?.Id,
        //            Voucher = voucher,
        //            VoucherCodeSnapshot = voucher?.Code,
        //            Items = new List<OrderItem>()
        //        };

        //        // 7) Copy items
        //        foreach (var it in cart.Items)
        //        {
        //            order.Items.Add(new OrderItem
        //            {
        //                ProductId = it.ProductId,
        //                Quantity = it.Quantity,
        //                UnitPrice = it.UnitPrice
        //            });
        //        }

        //        // 8) Chính sách trạng thái theo PaymentMethod
        //        order.Status = (req.PaymentMethod == PaymentMethod.COD)
        //            ? OrderStatus.Pending        // chờ giao/thu tiền
        //            : OrderStatus.Processing;    // đã tạo đơn, chờ xác nhận thanh toán online

        //        // 9) Trừ tồn kho (giữ hàng)
        //        foreach (var it in cart.Items)
        //            it.Product!.Quantity -= it.Quantity;

        //        // 10) Đánh dấu cart đã checkout
        //        cart.IsCheckedOut = true;
        //        cart.UpdatedAt = DateTime.UtcNow;

        //        // 11) Cập nhật usage voucher (nếu có) – nếu muốn chỉ tăng sau khi thanh toán thành công,
        //        // hãy dời khối này sang IPN/Webhook Momo
        //        if (voucher != null)
        //        {
        //            voucher.CurrentUsageCount += 1;
        //            voucher.UsedAt = DateTime.UtcNow;
        //            if (voucher.MaxUsageCount > 0 && voucher.CurrentUsageCount >= voucher.MaxUsageCount)
        //                voucher.IsValid = false;
        //        }

        //        _context.Orders.Add(order);
        //        await _context.SaveChangesAsync(ct);
        //        await tx.CommitAsync(ct);

        //        return Ok(new
        //        {
        //            Message = "Đặt hàng thành công!",
        //            OrderId = order.Id,
        //            PaymentMethod = order.PaymentMethod.ToString(),
        //            OrderStatus = order.Status.ToString(),
        //            Subtotal = order.TotalAmount,
        //            Discount = order.DiscountAmount,
        //            FinalAmount = order.FinalAmount,
        //            Voucher = order.VoucherCodeSnapshot
        //        });
        //    }
        //    catch
        //    {
        //        await tx.RollbackAsync(ct);
        //        throw;
        //    }
        //}

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