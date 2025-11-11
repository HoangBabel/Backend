using Backend.Services;
using System.Net.Mail;
using System.Net;
using Backend.Models;
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Backend.DTOs;

namespace Backend.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task Send2FACodeAsync(string toEmail, string code, string userName);
        Task SendOrderConfirmationEmailAsync(int orderId, CancellationToken ct = default);
        Task SendOrderStatusUpdateEmailAsync(int orderId, OrderStatus newStatus, CancellationToken ct = default);
        Task SendRentalConfirmationEmailAsync(int rentalId, CancellationToken ct = default);
        Task SendRentalStatusUpdateEmailAsync(int rentalId, RentalStatus newStatus, CancellationToken ct = default);
        Task SendRentalSettlementEmailAsync(
            int rentalId,
            int lateDays,
            decimal lateFee,
            decimal cleaningFee,
            decimal damageFee,
            decimal depositPaid,
            decimal depositRefund,
            CancellationToken ct = default);
    }
}
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    public EmailService(IConfiguration config , IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var fromEmail = _config["Email:FromEmail"] ?? throw new Exception("Email:FromEmail not configured");
        var password = _config["Email:Password"] ?? throw new Exception("Email:Password not configured");

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(fromEmail, password)
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, "Hệ thống bảo mật"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage);
    }

    public async Task Send2FACodeAsync(string toEmail, string code, string userName)
    {
        var subject = "Mã xác thực đăng nhập - 2FA";
        var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Xin chào {userName},</h2>
                    <p>Mã xác thực đăng nhập của bạn là:</p>
                    <h1 style='color: #4CAF50; font-size: 32px; letter-spacing: 5px;'>{code}</h1>
                    <p>Mã này có hiệu lực trong <strong>5 phút</strong>.</p>
                    <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                    <hr>
                    <p style='color: #999; font-size: 12px;'>Email tự động, vui lòng không trả lời.</p>
                </body>
                </html>
            ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendOrderConfirmationEmailAsync(int orderId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await GetOrderWithDetailsAsync(context, orderId, ct);
        if (order?.User?.Email == null) return;

        var subject = $"Xác nhận đơn hàng #{order.Id}";
        var body = BuildOrderConfirmationEmailBody(order);

        await SendEmailAsync(order.User.Email, subject, body);
    }

    public async Task SendOrderStatusUpdateEmailAsync(int orderId, OrderStatus newStatus, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await GetOrderWithDetailsAsync(context, orderId, ct);
        if (order?.User?.Email == null) return;

        var subject = $"Cập nhật đơn hàng #{order.Id} - {GetStatusText(newStatus)}";
        var body = BuildStatusUpdateEmailBody(order, newStatus);

        await SendEmailAsync(order.User.Email, subject, body);
    }

    private static async Task<Order?> GetOrderWithDetailsAsync(AppDbContext context, int orderId, CancellationToken ct)
    {
        return await context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Voucher)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    private static string BuildOrderConfirmationEmailBody(Order order)
    {
        var itemsHtml = string.Join("", order.Items.Select(item => $@"
            <tr>
                <td style='padding: 10px; border-bottom: 1px solid #e0e0e0;'>{item.Product?.Name ?? "N/A"}</td>
                <td style='padding: 10px; border-bottom: 1px solid #e0e0e0; text-align: center;'>{item.Quantity}</td>
                <td style='padding: 10px; border-bottom: 1px solid #e0e0e0; text-align: right;'>{item.UnitPrice:N0}đ</td>
                <td style='padding: 10px; border-bottom: 1px solid #e0e0e0; text-align: right; font-weight: bold;'>{(item.UnitPrice * item.Quantity):N0}đ</td>
            </tr>
        "));

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4;'>
    <div style='max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
        
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%); color: white; padding: 30px 20px; text-align: center;'>
            <h1 style='margin: 0; font-size: 28px;'>✅ Đặt hàng thành công!</h1>
            <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Cảm ơn bạn đã tin tưởng chúng tôi</p>
        </div>
        
        <!-- Content -->
        <div style='padding: 30px 20px;'>
            <p style='font-size: 16px; margin: 0 0 20px 0;'>Xin chào <strong style='color: #4CAF50;'>{order.User.FullName ?? order.User.Email}</strong>,</p>
            <p style='font-size: 14px; color: #666; margin: 0 0 25px 0;'>
                Đơn hàng của bạn đã được tiếp nhận và đang chờ xử lý. Chúng tôi sẽ thông báo cho bạn khi đơn hàng được giao đi.
            </p>
            
            <!-- Order Info Box -->
            <div style='background-color: #f9f9f9; border-left: 4px solid #4CAF50; padding: 15px; margin-bottom: 25px; border-radius: 4px;'>
                <h3 style='margin: 0 0 10px 0; color: #333; font-size: 18px;'>
                    📦 Đơn hàng #<span style='color: #4CAF50;'>{order.Id}</span>
                </h3>
                <p style='margin: 5px 0; font-size: 14px;'><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>
                <p style='margin: 5px 0; font-size: 14px;'><strong>Trạng thái:</strong> <span style='color: #FF9800; font-weight: bold;'>{GetStatusText(order.Status)}</span></p>
                <p style='margin: 5px 0; font-size: 14px;'><strong>Thanh toán:</strong> {GetPaymentMethodText(order.PaymentMethod)}</p>
            </div>
            
            <!-- Shipping Address -->
            <div style='margin-bottom: 25px;'>
                <h3 style='color: #333; font-size: 16px; margin: 0 0 10px 0;'>🚚 Địa chỉ giao hàng</h3>
                <p style='font-size: 14px; color: #666; margin: 0; padding: 10px; background-color: #f9f9f9; border-radius: 4px;'>
                    {order.ShippingAddress}
                </p>
            </div>
            
            <!-- Products Table -->
            <h3 style='color: #333; font-size: 16px; margin: 0 0 15px 0;'>📋 Chi tiết sản phẩm</h3>
            <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px; background-color: #fff;'>
                <thead>
                    <tr style='background-color: #4CAF50; color: white;'>
                        <th style='padding: 12px 10px; text-align: left; font-size: 14px;'>Sản phẩm</th>
                        <th style='padding: 12px 10px; text-align: center; font-size: 14px;'>SL</th>
                        <th style='padding: 12px 10px; text-align: right; font-size: 14px;'>Đơn giá</th>
                        <th style='padding: 12px 10px; text-align: right; font-size: 14px;'>Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>
            
            <!-- Summary -->
            <div style='background-color: #f9f9f9; padding: 15px; border-radius: 4px;'>
                <table style='width: 100%; font-size: 14px;'>
                    <tr>
                        <td style='padding: 5px 0; text-align: right; color: #666;'>Tạm tính:</td>
                        <td style='padding: 5px 0; text-align: right; width: 120px;'>{order.TotalAmount:N0}đ</td>
                    </tr>
                    <tr>
                        <td style='padding: 5px 0; text-align: right; color: #666;'>Phí vận chuyển:</td>
                        <td style='padding: 5px 0; text-align: right;'>{order.ShippingFee:N0}đ</td>
                    </tr>
                    {(order.DiscountAmount > 0 ? $@"
                    <tr>
                        <td style='padding: 5px 0; text-align: right; color: #666;'>Giảm giá ({order.VoucherCodeSnapshot}):</td>
                        <td style='padding: 5px 0; text-align: right; color: #f44336; font-weight: bold;'>-{order.DiscountAmount:N0}đ</td>
                    </tr>
                    " : "")}
                    <tr style='border-top: 2px solid #4CAF50;'>
                        <td style='padding: 10px 0 0 0; text-align: right; font-size: 16px; font-weight: bold;'>TỔNG CỘNG:</td>
                        <td style='padding: 10px 0 0 0; text-align: right; font-size: 18px; font-weight: bold; color: #4CAF50;'>{order.FinalAmount:N0}đ</td>
                    </tr>
                </table>
            </div>
            
            <!-- Note -->
            <div style='margin-top: 25px; padding: 15px; background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 4px;'>
                <p style='margin: 0; font-size: 13px; color: #856404;'>
                    <strong>💡 Lưu ý:</strong> Nếu có bất kỳ thắc mắc nào về đơn hàng, vui lòng liên hệ với chúng tôi qua email hoặc hotline.
                </p>
            </div>
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f4f4f4; padding: 20px; text-align: center; border-top: 1px solid #e0e0e0;'>
            <p style='margin: 0 0 5px 0; font-size: 12px; color: #999;'>Email này được gửi tự động, vui lòng không trả lời.</p>
            <p style='margin: 0; font-size: 12px; color: #999;'>&copy; 2025 Your Company. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
        ";
    }

    private static string BuildStatusUpdateEmailBody(Order order, OrderStatus newStatus)
    {
        var (statusColor, statusIcon, statusMessage) = newStatus switch
        {
            OrderStatus.Processing => ("#FF9800", "⏳", "Đơn hàng của bạn đang được xử lý và chuẩn bị giao."),
            OrderStatus.Completed => ("#4CAF50", "✅", "Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua hàng!"),
            OrderStatus.Cancelled => ("#f44336", "❌", "Đơn hàng của bạn đã bị hủy. Nếu có thắc mắc, vui lòng liên hệ với chúng tôi."),
            _ => ("#666", "ℹ️", "Trạng thái đơn hàng của bạn đã được cập nhật.")
        };

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4;'>
    <div style='max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
        
        <!-- Header -->
        <div style='background: linear-gradient(135deg, {statusColor} 0%, {statusColor}dd 100%); color: white; padding: 30px 20px; text-align: center;'>
            <h1 style='margin: 0; font-size: 28px;'>{statusIcon} Cập nhật đơn hàng</h1>
        </div>
        
        <!-- Content -->
        <div style='padding: 30px 20px;'>
            <p style='font-size: 16px; margin: 0 0 20px 0;'>Xin chào <strong style='color: {statusColor};'>{order.User.FullName ?? order.User.Email}</strong>,</p>
            
            <!-- Status Box -->
            <div style='text-align: center; margin: 30px 0; padding: 25px; background: linear-gradient(135deg, {statusColor}15 0%, {statusColor}05 100%); border-radius: 8px; border: 2px solid {statusColor};'>
                <p style='margin: 0 0 10px 0; font-size: 14px; color: #666;'>Đơn hàng</p>
                <h2 style='margin: 0 0 15px 0; font-size: 24px; color: {statusColor};'>#{order.Id}</h2>
                <div style='display: inline-block; padding: 10px 20px; background-color: {statusColor}; color: white; border-radius: 20px; font-weight: bold; font-size: 16px;'>
                    {GetStatusText(newStatus)}
                </div>
            </div>
            
            <p style='font-size: 15px; color: #666; text-align: center; margin: 0 0 30px 0; line-height: 1.8;'>
                {statusMessage}
            </p>
            
            <!-- Order Details -->
            <div style='background-color: #f9f9f9; padding: 20px; border-radius: 4px; margin-bottom: 20px;'>
                <h3 style='margin: 0 0 15px 0; color: #333; font-size: 16px; border-bottom: 2px solid {statusColor}; padding-bottom: 10px;'>
                    📦 Thông tin đơn hàng
                </h3>
                <table style='width: 100%; font-size: 14px;'>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Ngày đặt:</td>
                        <td style='padding: 8px 0; text-align: right; font-weight: bold;'>{order.OrderDate:dd/MM/yyyy HH:mm}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Tổng tiền:</td>
                        <td style='padding: 8px 0; text-align: right; font-size: 18px; font-weight: bold; color: {statusColor};'>{order.FinalAmount:N0}đ</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #666;'>Thanh toán:</td>
                        <td style='padding: 8px 0; text-align: right;'>{GetPaymentMethodText(order.PaymentMethod)}</td>
                    </tr>
                </table>
            </div>
            
            <!-- Shipping Address -->
            <div style='background-color: #f9f9f9; padding: 20px; border-radius: 4px;'>
                <h3 style='margin: 0 0 10px 0; color: #333; font-size: 16px;'>🚚 Địa chỉ giao hàng</h3>
                <p style='margin: 0; font-size: 14px; color: #666;'>{order.ShippingAddress}</p>
            </div>
            
            {(newStatus == OrderStatus.Completed ? $@"
            <!-- Thank You Message -->
            <div style='margin-top: 25px; padding: 20px; background: linear-gradient(135deg, #4CAF5015 0%, #4CAF5005 100%); border-left: 4px solid #4CAF50; border-radius: 4px; text-align: center;'>
                <h3 style='margin: 0 0 10px 0; color: #4CAF50; font-size: 20px;'>🎉 Cảm ơn bạn!</h3>
                <p style='margin: 0; font-size: 14px; color: #666;'>
                    Chúng tôi hy vọng bạn hài lòng với sản phẩm.<br>
                    Đừng quên để lại đánh giá để giúp chúng tôi cải thiện dịch vụ nhé!
                </p>
            </div>
            " : "")}
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f4f4f4; padding: 20px; text-align: center; border-top: 1px solid #e0e0e0;'>
            <p style='margin: 0 0 5px 0; font-size: 12px; color: #999;'>Email này được gửi tự động, vui lòng không trả lời.</p>
            <p style='margin: 0; font-size: 12px; color: #999;'>&copy; 2025 Your Company. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
        ";
    }

    private static string GetStatusText(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Đang chờ xử lý",
        OrderStatus.Processing => "Đang xử lý",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    private static string GetPaymentMethodText(PaymentMethod method) => method switch
    {
        PaymentMethod.COD => "Thanh toán khi nhận hàng (COD)",
        PaymentMethod.QR => "Chuyển khoản QR",
        _ => method.ToString()
    };

    // ===== RENTAL EMAIL METHODS =====

    public async Task SendRentalConfirmationEmailAsync(int rentalId, CancellationToken ct = default)
    {
        // ✅ TẠO SCOPE ĐỂ LẤY AppDbContext
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rental = await context.Rentals
            .Include(r => r.User)
            .Include(r => r.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == rentalId, ct);

        if (rental?.User?.Email == null) return;

        var subject = $"Xác nhận đơn thuê #{rentalId}";
        var itemsHtml = string.Join("", rental.Items.Select(item => $@"
                <tr>
                    <td style='padding: 8px; border-bottom: 1px solid #e5e7eb;'>{item.Product?.Name ?? "N/A"}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #e5e7eb; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #e5e7eb; text-align: right;'>{item.SubTotal:N0} đ</td>
                </tr>
            "));

        var rentalDays = (rental.EndDate - rental.StartDate).Days;
        var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>🎉 Xác nhận đơn thuê</h2>
                    <p>Xin chào <strong>{rental.User.FullName}</strong>,</p>
                    <p>Đơn thuê <strong>#{rentalId}</strong> của bạn đã được tạo thành công!</p>
                    
                    <div style='background: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='margin-top: 0;'>Thông tin đơn thuê</h3>
                        <p style='margin: 5px 0;'><strong>Mã đơn:</strong> #{rentalId}</p>
                        <p style='margin: 5px 0;'><strong>Ngày bắt đầu:</strong> {rental.StartDate:dd/MM/yyyy HH:mm}</p>
                        <p style='margin: 5px 0;'><strong>Ngày kết thúc:</strong> {rental.EndDate:dd/MM/yyyy HH:mm}</p>
                        <p style='margin: 5px 0;'><strong>Số ngày thuê:</strong> {rentalDays} ngày</p>
                        <p style='margin: 5px 0;'><strong>Địa chỉ giao hàng:</strong> {rental.ShippingAddress}</p>
                        <p style='margin: 5px 0;'><strong>Trạng thái:</strong> <span style='color: #f59e0b; font-weight: bold;'>Đang chờ xác nhận</span></p>
                    </div>

                    <h3>Chi tiết sản phẩm</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background: #e5e7eb;'>
                                <th style='padding: 10px; text-align: left;'>Sản phẩm</th>
                                <th style='padding: 10px; text-align: center;'>SL</th>
                                <th style='padding: 10px; text-align: right;'>Giá thuê</th>
                                <th style='padding: 10px; text-align: right;'>Tổng</th>
                            </tr>
                        </thead>
                        <tbody>{itemsHtml}</tbody>
                    </table>

                    <div style='margin-top: 20px; padding: 15px; background: #fef3c7; border-radius: 8px;'>
                        <p style='margin: 5px 0;'><strong>Tổng tiền thuê:</strong> <span style='font-size: 18px; color: #2563eb;'>{rental.TotalPrice:N0} đ</span></p>
                        <p style='margin: 5px 0;'><strong>Tiền cọc:</strong> {rental.DepositPaid:N0} đ</p>
                        <p style='margin: 5px 0;'><strong>Phí vận chuyển:</strong> {rental.ShippingFee:N0} đ</p>
                    </div>

                    <p style='margin-top: 20px; color: #6b7280;'>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                </div>
            ";

        await SendEmailAsync(rental.User.Email, subject, body);
    }

    public async Task SendRentalStatusUpdateEmailAsync(int rentalId, RentalStatus newStatus, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rental = await context.Rentals
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == rentalId, ct);

        if (rental?.User?.Email == null) return;

        var (subject, statusText, statusColor, icon) = newStatus switch
        {
            RentalStatus.Active => ($"Đơn thuê #{rentalId} đã được kích hoạt", "Đang thuê", "#10b981", "✅"),
            RentalStatus.Completed => ($"Đơn thuê #{rentalId} đã hoàn thành", "Đã hoàn thành", "#6366f1", "🎉"),
            RentalStatus.Cancelled => ($"Đơn thuê #{rentalId} đã bị hủy", "Đã hủy", "#ef4444", "❌"),
            _ => ($"Cập nhật đơn thuê #{rentalId}", "Đang chờ xác nhận", "#f59e0b", "⏳")
        };

        var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>{icon} Cập nhật trạng thái đơn thuê</h2>
                    <p>Xin chào <strong>{rental.User.FullName}</strong>,</p>
                    <p>Đơn thuê <strong>#{rentalId}</strong> của bạn đã được cập nhật.</p>
                    
                    <div style='background: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Trạng thái mới:</strong> <span style='color: {statusColor}; font-weight: bold; font-size: 16px;'>{statusText}</span></p>
                        <p style='margin: 5px 0;'><strong>Ngày bắt đầu:</strong> {rental.StartDate:dd/MM/yyyy HH:mm}</p>
                        <p style='margin: 5px 0;'><strong>Ngày kết thúc:</strong> {rental.EndDate:dd/MM/yyyy HH:mm}</p>
                    </div>

                    <p style='margin-top: 20px; color: #6b7280;'>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                </div>
            ";

        await SendEmailAsync(rental.User.Email, subject, body);
    }

    public async Task SendRentalSettlementEmailAsync(
        int rentalId,
        int lateDays,
        decimal lateFee,
        decimal cleaningFee,
        decimal damageFee,
        decimal depositPaid,
        decimal depositRefund,
        CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rental = await context.Rentals
            .Include(r => r.User)
            .Include(r => r.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(r => r.Id == rentalId, ct);

        if (rental?.User?.Email == null) return;

        var subject = $"Quyết toán đơn thuê #{rentalId}";
        var totalDeductions = lateFee + cleaningFee + damageFee;

        var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>💰 Quyết toán đơn thuê</h2>
                    <p>Xin chào <strong>{rental.User.FullName}</strong>,</p>
                    <p>Đơn thuê <strong>#{rentalId}</strong> của bạn đã được quyết toán.</p>
                    
                    <div style='background: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='margin-top: 0;'>Thông tin quyết toán</h3>
                        <p style='margin: 5px 0;'><strong>Ngày trả:</strong> {rental.ReturnedAt:dd/MM/yyyy HH:mm}</p>
                        <p style='margin: 5px 0;'><strong>Ngày hết hạn:</strong> {rental.EndDate:dd/MM/yyyy HH:mm}</p>
                        {(lateDays > 0 ? $"<p style='margin: 5px 0; color: #ef4444;'><strong>⚠️ Số ngày trễ:</strong> {lateDays} ngày</p>" : "<p style='margin: 5px 0; color: #10b981;'><strong>✅ Trả đúng hạn</strong></p>")}
                    </div>

                    <h3>Chi tiết phí</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr style='border-bottom: 1px solid #e5e7eb;'>
                            <td style='padding: 10px;'>Tiền cọc ban đầu</td>
                            <td style='padding: 10px; text-align: right; font-weight: bold;'>{depositPaid:N0} đ</td>
                        </tr>
                        {(lateFee > 0 ? $@"<tr style='border-bottom: 1px solid #e5e7eb;'><td style='padding: 10px; color: #ef4444;'>Phí trễ hạn ({lateDays} ngày)</td><td style='padding: 10px; text-align: right; color: #ef4444;'>-{lateFee:N0} đ</td></tr>" : "")}
                        {(cleaningFee > 0 ? $@"<tr style='border-bottom: 1px solid #e5e7eb;'><td style='padding: 10px; color: #ef4444;'>Phí vệ sinh</td><td style='padding: 10px; text-align: right; color: #ef4444;'>-{cleaningFee:N0} đ</td></tr>" : "")}
                        {(damageFee > 0 ? $@"<tr style='border-bottom: 1px solid #e5e7eb;'><td style='padding: 10px; color: #ef4444;'>Phí hư hỏng</td><td style='padding: 10px; text-align: right; color: #ef4444;'>-{damageFee:N0} đ</td></tr>" : "")}
                        {(totalDeductions > 0 ? $@"<tr style='border-bottom: 2px solid #e5e7eb; background: #fef2f2;'><td style='padding: 10px; font-weight: bold;'>Tổng khấu trừ</td><td style='padding: 10px; text-align: right; font-weight: bold; color: #ef4444;'>-{totalDeductions:N0} đ</td></tr>" : "")}
                        <tr style='background: {(depositRefund > 0 ? "#d1fae5" : "#fee2e2")};'>
                            <td style='padding: 15px; font-weight: bold; font-size: 16px;'>Số tiền hoàn lại</td>
                            <td style='padding: 15px; text-align: right; font-weight: bold; color: {(depositRefund > 0 ? "#10b981" : "#ef4444")}; font-size: 20px;'>{depositRefund:N0} đ</td>
                        </tr>
                    </table>

                    <div style='margin-top: 20px; padding: 15px; background: {(depositRefund > 0 ? "#d1fae5" : "#fee2e2")}; border-radius: 8px; border-left: 4px solid {(depositRefund > 0 ? "#10b981" : "#ef4444")};'>
                        <p style='margin: 0;'>
                            {(depositRefund > 0
                        ? $"✅ Số tiền <strong>{depositRefund:N0} đ</strong> sẽ được hoàn lại vào tài khoản của bạn trong <strong>3-5 ngày làm việc</strong>."
                        : $"⚠️ Tiền cọc đã được khấu trừ hoàn toàn do các khoản phí phát sinh (<strong>{totalDeductions:N0} đ</strong>).")}
                        </p>
                    </div>

                    <p style='margin-top: 20px; color: #6b7280;'>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                </div>
            ";

        await SendEmailAsync(rental.User.Email, subject, body);
    }
}