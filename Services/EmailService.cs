using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MemmoApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailVerificationAsync(string toEmail, string toName, string verificationLink)
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"]!;
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]!);
            var senderEmail = _configuration["EmailSettings:SenderEmail"]!;
            var senderName = _configuration["EmailSettings:SenderName"]!;
            var userName = _configuration["EmailSettings:UserName"]!;
            var password = _configuration["EmailSettings:Password"]!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "ยืนยันอีเมลของคุณ - Memmo";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8' />
  <style>
    body {{ font-family: 'Segoe UI', sans-serif; background: #f4f6fb; margin: 0; padding: 0; }}
    .container {{ max-width: 520px; margin: 40px auto; background: #fff; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); overflow: hidden; }}
    .header {{ background: #6c63ff; padding: 32px 24px; text-align: center; }}
    .header h1 {{ color: #fff; margin: 0; font-size: 26px; letter-spacing: 1px; }}
    .body {{ padding: 32px 24px; }}
    .body p {{ color: #444; font-size: 15px; line-height: 1.6; }}
    .btn {{ display: inline-block; margin: 24px 0 8px; padding: 14px 36px; background: #6c63ff; color: #fff !important; text-decoration: none; border-radius: 8px; font-size: 16px; font-weight: 600; }}
    .footer {{ background: #f4f6fb; padding: 16px 24px; text-align: center; color: #aaa; font-size: 12px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'><h1>Memmo</h1></div>
    <div class='body'>
      <p>สวัสดี <strong>{toName}</strong>,</p>
      <p>ขอบคุณที่สมัครสมาชิก Memmo! กรุณากดปุ่มด้านล่างเพื่อยืนยันอีเมลของคุณ</p>
      <p style='text-align:center;'>
        <a href='{verificationLink}' class='btn'>ยืนยันอีเมล</a>
      </p>
      <p>ลิงก์นี้จะหมดอายุใน <strong>24 ชั่วโมง</strong></p>
      <p>หากคุณไม่ได้สมัครสมาชิก กรุณาเพิกเฉยต่ออีเมลนี้</p>
    </div>
    <div class='footer'>© 2026 Memmo. All rights reserved.</div>
  </div>
</body>
</html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(userName, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
