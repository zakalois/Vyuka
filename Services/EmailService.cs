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

        // Jednoduché odeslání
        public async Task SendAsync(string to, string subject, string html)
        {
            await SendAsync(to, subject, html, null, null, null, null, null, "system", null);
        }

        // Odeslání s přílohami
        public async Task SendAsync(string to, string subject, string html, List<EmailAttachment>? attachments)
        {
            await SendAsync(to, subject, html, attachments, null, null, null, null, "system", null);
        }

        // ⭐ ROZŠÍŘENÁ METODA – placeholdery + QR + logování
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
            // ⭐ OČIŠTĚNÍ EMAILU
            to = to?
                .Trim()
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace("\uFEFF", "");

            if (string.IsNullOrWhiteSpace(to))
                throw new Exception("EmailService: prázdná nebo neplatná emailová adresa.");

            // ⭐ NAHRAZENÍ PLACEHOLDERŮ
            html = html.Replace("{{Amount}}", dynamicAmount?.ToString("0") ?? "");
            html = html.Replace("{{Message}}", dynamicMessage ?? "");
            html = html.Replace("{{CustomText}}", customText ?? "");
            html = html.Replace("{{StudentName}}", studentName ?? "");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Výuka App", _smtp.From));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = html
            };

            // ⭐ LOGO
            AddImageFromFile(builder, "logo", Path.Combine(_env.WebRootPath, "images", "logo.jpg"));

            // ❌ ODSTRANĚNO – statické QR obrázky
            // AddImageFromFile(builder, "qr350", ...);
            // AddImageFromFile(builder, "qr400", ...);
            // AddImageFromFile(builder, "qr3500", ...);
            // AddImageFromFile(builder, "qr4000", ...);

            // ⭐ Dynamický QR kód – generovat vždy
            bool hasQr = false;

            if (dynamicAmount.HasValue)
            {
                var qrBytes = _qr.GeneratePaymentQr(dynamicAmount.Value, dynamicMessage ?? "");
                AddImageFromBytes(builder, "qrDynamic", qrBytes);
                hasQr = true;
            }

            // ⭐ Přílohy
            if (attachments != null)
            {
                foreach (var att in attachments)
                {
                    if (File.Exists(att.FilePath))
                        builder.Attachments.Add(att.FilePath);
                }
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtp.User, _smtp.Password);
                await client.SendAsync(message);

                // ⭐ LOGOVÁNÍ ÚSPĚCHU
                await LogEmailAsync(
                    to,
                    subject,
                    html,
                    emailType,
                    success: true,
                    error: null,
                    hasQr,
                    studentName,
                    studentId
                );
            }
            catch (Exception ex)
            {
                // ⭐ LOGOVÁNÍ CHYBY
                await LogEmailAsync(
                    to,
                    subject,
                    html,
                    emailType,
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

        // ⭐ PROFESIONÁLNÍ LOGOVÁNÍ
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

        // Obrázek ze souboru
        private void AddImageFromFile(BodyBuilder builder, string contentId, string path)
        {
            if (File.Exists(path))
            {
                var img = builder.LinkedResources.Add(path);
                img.ContentId = contentId;
                img.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            }
        }

        // Obrázek z byte[] (dynamický QR)
        private void AddImageFromBytes(BodyBuilder builder, string contentId, byte[] bytes)
        {
            var img = builder.LinkedResources.Add(contentId + ".png", bytes, new ContentType("image", "png"));
            img.ContentId = contentId;
            img.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
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
    }
}
