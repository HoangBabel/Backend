using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public OrderController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: api/Order
        [HttpGet]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderStatus? status = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Voucher)
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return Ok(orders);
        }


        // GET: api/Order/User
        // Lấy đơn hàng của user hiện tại dựa trên token
        [HttpGet("User")]
        public async Task<IActionResult> GetOrdersByCurrentUser()
        {
            // Lấy userId từ Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { Message = "Token không hợp lệ hoặc chưa đăng nhập" });
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest(new { Message = "UserId không hợp lệ" });
            }

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            return Ok(orders);
        }

        // Các endpoint khác giữ nguyên
        // PUT: api/Order/Payment/{orderId}
        [HttpPut("Payment/{orderId}")]
        public async Task<IActionResult> MarkAsPaid(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { Message = "Không tìm thấy đơn hàng" });

            var oldStatus = order.Status;
            order.Status = OrderStatus.Completed;
            await _context.SaveChangesAsync();

            if (oldStatus != OrderStatus.Completed)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendOrderStatusUpdateEmailAsync(orderId, OrderStatus.Completed, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send payment confirmation email for order {orderId}: {ex.Message}");
                    }
                });
            }

            return Ok(new { Message = "Thanh toán thành công! Email xác nhận đã được gửi." });
        }

        // PUT: api/Order/Status/{orderId}
        [HttpPut("Status/{orderId}")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { Message = "Không tìm thấy đơn hàng" });

            var oldStatus = order.Status;
            if (oldStatus == OrderStatus.Completed && request.Status != OrderStatus.Completed)
                return BadRequest(new { Message = "Không thể thay đổi trạng thái đơn hàng đã hoàn thành" });

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            if (oldStatus != request.Status)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendOrderStatusUpdateEmailAsync(orderId, request.Status, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send status update email for order {orderId}: {ex.Message}");
                    }
                });
            }

            return Ok(new
            {
                Message = $"Cập nhật trạng thái thành công: {GetStatusText(request.Status)}",
                OldStatus = oldStatus,
                NewStatus = request.Status
            });
        }

        // GET: api/Order/{orderId}
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Voucher)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound(new { Message = "Không tìm thấy đơn hàng" });

            return Ok(order);
        }

        private static string GetStatusText(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "Đang chờ xử lý",
            OrderStatus.Processing => "Đang xử lý",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }

    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }
    }
}
