using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Vyuka.Models;

namespace Vyuka.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;
        private readonly IWebHostEnvironment _env;
        private readonly QrCodeGeneratorService _qr;
        private readonly AppDbContext _context;

        public EmailService(
            IOptions<SmtpSettings> smtp,
            IWebHostEnvironment env,
            QrCodeGeneratorService qr,
            AppDbContext context)
        {
            _smtp = smtp.Value;
            _env = env;
            _qr = qr;
            _context = context;
        }

        public async Task SendAsync(string to, string subject, string html)
        {
            await SendAsync(to, subject, html, null, null, null, null, null, "system", null);
        }

        public async Task SendAsync(string to, string subject, string html, List<EmailAttachment>? attachments)
        {
            await SendAsync(to, subject, html, attachments, null, null, null, null, "system", null);
        }

        public async Task SendAsync(
            string to,
            string subject,
            string html,
            List<EmailAttachment>? attachments,
            decimal? dynamicAmount,
            string? dynamicMessage,
            string? customText,
            string? studentName,
            string emailType,
            int? studentId)
        {
            to = to?
                .Trim()
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace("\uFEFF", "");

            if (string.IsNullOrWhiteSpace(to))
                throw new Exception("EmailService: prázdná nebo neplatná emailová adresa.");

            html = html.Replace("{{Amount}}", dynamicAmount?.ToString("0") ?? "");
            html = html.Replace("{{Message}}", dynamicMessage ?? "");
            html = html.Replace("{{CustomText}}", customText ?? "");
            html = html.Replace("{{StudentName}}", studentName ?? "");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Výuka App", _smtp.From));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            // -----------------------------
            // multipart/related (HTML + inline obrázky)
            // -----------------------------
            var related = new Multipart("related");

            // ⭐ Firemní podpis
            string signature = @"
<table style=""font-family:Segoe UI,Arial,sans-serif; font-size:14px; color:#333; margin-top:25px;"">
    <tr>
        <td style=""padding-right:15px; vertical-align:top;"">
           <img src=""cid:logoSmall"" alt=""Logo"" width=""75"" style=""border-radius:6px;"">
        </td>
        <td style=""vertical-align:top;"">
            <strong>Ing. Alois Žák</strong><br>
            <span style=""color:#555;"">+420 601 172 322</span><br>
            <a href=""https://zakalois.webnode.cz/"" style=""color:#6a0dad; text-decoration:none;"">zakalois.webnode.cz</a><br>
            <a href=""https://ucitelzak.eu/"" style=""color:#6a0dad; text-decoration:none;"">ucitelzak.eu</a>
        </td>
    </tr>
</table>
";

            html += signature;

            // ⭐ HTML část MUSÍ být první
            var htmlPart = new TextPart("html")
            {
                Text = html
            };
            related.Add(htmlPart);

            // ⭐ Malé logo pro podpis (logoSmall)
            string smallLogoPath = Path.Combine(_env.WebRootPath, "images", "logo.jpg");
            if (File.Exists(smallLogoPath))
            {
                var smallLogo = new MimePart("image", "jpeg")
                {
                    Content = new MimeContent(File.OpenRead(smallLogoPath)),
                    ContentId = "logoSmall",
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64
                };
                related.Add(smallLogo);
            }

            // ⭐ QR kód
            bool hasQr = false;
            if (dynamicAmount.HasValue)
            {
                var qrBytes = _qr.GeneratePaymentQr(dynamicAmount.Value, dynamicMessage ?? "");
                var qr = new MimePart("image", "png")
                {
                    Content = new MimeContent(new MemoryStream(qrBytes)),
                    ContentId = "qrDynamic",
                    ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64
                };
                related.Add(qr);
                hasQr = true;
            }

            // -----------------------------
            // multipart/mixed (related + attachments)
            // -----------------------------
            var mixed = new Multipart("mixed");
            mixed.Add(related);

            if (attachments != null)
            {
                foreach (var att in attachments)
                {
                    var attachmentPart = new MimePart(att.ContentType ?? "application/octet-stream")
                    {
                        Content = new MimeContent(new MemoryStream(att.Content)),
                        FileName = att.FileName,
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64
                    };

                    mixed.Add(attachmentPart);
                }
            }

            message.Body = mixed;

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtp.User, _smtp.Password);
                await client.SendAsync(message);

                await LogEmailAsync(
                    to, subject, html, emailType,
                    success: true,
                    error: null,
                    hasQr,
                    studentName,
                    studentId
                );
            }
            catch (Exception ex)
            {
                await LogEmailAsync(
                    to, subject, html, emailType,
                    success: false,
                    error: ex.Message,
                    hasQr,
                    studentName,
                    studentId
                );

                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        public async Task SendPasswordResetEmail(string email, string name, string token)
        {
            var resetLink = $"https://{_smtp.AppDomain}/Account/ResetPassword?token={token}";

            string html = $@"
<p>Ahoj {name},</p>
<p>Klikni na tento odkaz pro reset hesla:</p>
<p><a href=""{resetLink}"">{resetLink}</a></p>
<p>Odkaz je platný 1 hodinu.</p>
<p>Výuka App</p>";

            await SendAsync(email, "Reset hesla", html);
        }

        private async Task LogEmailAsync(
            string to,
            string subject,
            string html,
            string emailType,
            bool success,
            string? error,
            bool hasQr,
            string? studentName,
            int? studentId)
        {
            var log = new EmailLog
            {
                SentAt = DateTime.Now,
                Recipient = to,
                Subject = subject,
                Html = html,
                EmailType = emailType,
                Success = success,
                ErrorMessage = error,
                HasQrCode = hasQr,
                StudentName = studentName,
                StudentId = studentId,
                SentBy = "system"
            };

            _context.EmailLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
