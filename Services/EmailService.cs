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

        public EmailService(
            IOptions<SmtpSettings> smtp,
            IWebHostEnvironment env,
            QrCodeGeneratorService qr)
        {
            _smtp = smtp.Value;
            _env = env;
            _qr = qr;
        }

        // ⭐ 1) Jednoduché odeslání bez příloh
        public async Task SendAsync(string to, string subject, string html)
        {
            await SendAsync(to, subject, html, null, null, null);
        }

        // ⭐ 2) PŮVODNÍ METODA z IEmailService (nutná!)
        public async Task SendAsync(string to, string subject, string html, List<EmailAttachment>? attachments)
        {
            await SendAsync(to, subject, html, attachments, null, null);
        }

        // ⭐ 3) Nová rozšířená metoda s dynamickým QR
        public async Task SendAsync(
            string to,
            string subject,
            string html,
            List<EmailAttachment>? attachments,
            decimal? dynamicAmount,
            string? dynamicMessage)
        {
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

            // ⭐ Statické QR obrázky
            AddImageFromFile(builder, "qr350", Path.Combine(_env.WebRootPath, "images", "QR", "1_hod_350.jpg"));
            AddImageFromFile(builder, "qr400", Path.Combine(_env.WebRootPath, "images", "QR", "1_hod_400.jpg"));
            AddImageFromFile(builder, "qr3500", Path.Combine(_env.WebRootPath, "images", "QR", "10_hod_3500.jpg"));
            AddImageFromFile(builder, "qr4000", Path.Combine(_env.WebRootPath, "images", "QR", "10_hod_4000.jpg"));

            // ⭐ Dynamický QR kód
            if (dynamicAmount.HasValue && !string.IsNullOrWhiteSpace(dynamicMessage))
            {
                var qrBytes = _qr.GeneratePaymentQr(
    dynamicAmount.Value,
    dynamicMessage
);


                AddImageFromBytes(builder, "qrDynamic", qrBytes);
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
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        // ⭐ Obrázek ze souboru
        private void AddImageFromFile(BodyBuilder builder, string contentId, string path)
        {
            if (File.Exists(path))
            {
                var img = builder.LinkedResources.Add(path);
                img.ContentId = contentId;
                img.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            }
        }

        // ⭐ Obrázek z byte[] (dynamický QR)
        private void AddImageFromBytes(BodyBuilder builder, string contentId, byte[] bytes)
        {
            var img = builder.LinkedResources.Add(contentId + ".png", bytes, new ContentType("image", "png"));
            img.ContentId = contentId;
            img.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
        }


        // ⭐ Reset hesla
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
