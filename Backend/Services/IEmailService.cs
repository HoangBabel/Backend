using Backend.Services;
using System.Net.Mail;
using System.Net;

namespace Backend.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task Send2FACodeAsync(string toEmail, string code, string userName);
    }
}
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "465");
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
}