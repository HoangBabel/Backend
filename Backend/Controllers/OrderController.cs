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
                return NotFound(new { Message = "Không tìm thấy đơn hàng" });

            // Lưu trạng thái cũ để kiểm tra
            var oldStatus = order.Status;

            // Cập nhật trạng thái
            order.Status = OrderStatus.Completed;
            await _context.SaveChangesAsync();

            // Gửi email thông báo (background task để không làm chậm response)
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
                        // Log lỗi nhưng không throw để không ảnh hưởng đến API response
                        Console.WriteLine($"Failed to send payment confirmation email for order {orderId}: {ex.Message}");
                    }
                }, CancellationToken.None);
            }

            return Ok(new { Message = "Thanh toán thành công! Email xác nhận đã được gửi." });
        }

        // PUT: api/Order/Status/5
    
        [HttpPut("Status/{orderId}")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { Message = "Không tìm thấy đơn hàng" });

            var oldStatus = order.Status;

            // Validate: Không cho cập nhật nếu đã Completed
            if (oldStatus == OrderStatus.Completed && request.Status != OrderStatus.Completed)
                return BadRequest(new { Message = "Không thể thay đổi trạng thái đơn hàng đã hoàn thành" });

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            // ✅ GỬI EMAIL KHI TRẠNG THÁI THAY ĐỔI
            if (oldStatus != request.Status)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendOrderStatusUpdateEmailAsync(
                            orderId,
                            request.Status,
                            CancellationToken.None
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to send status update email for order {orderId}: {ex.Message}");
                    }
                }, CancellationToken.None);
            }

            return Ok(new
            {
                Message = $"Cập nhật trạng thái thành công: {GetStatusText(request.Status)}",
                OldStatus = oldStatus,
                NewStatus = request.Status
            });
        }

        // GET: api/Order/5
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

        // GET: api/Order
        [HttpGet]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderStatus? status = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        // DELETE: api/Order/5
        [HttpDelete("{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { Message = "Không tìm thấy đơn hàng" });

            if (order.Status == OrderStatus.Completed)
                return BadRequest(new { Message = "Không thể hủy đơn hàng đã hoàn thành" });

            var oldStatus = order.Status;
            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            // Gửi email thông báo hủy đơn
            if (oldStatus != OrderStatus.Cancelled)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendOrderStatusUpdateEmailAsync(orderId, OrderStatus.Cancelled, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send cancellation email for order {orderId}: {ex.Message}");
                    }
                }, CancellationToken.None);
            }

            return Ok(new { Message = "Đơn hàng đã được hủy" });
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

    // DTO cho request cập nhật trạng thái
    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }
    }
}
