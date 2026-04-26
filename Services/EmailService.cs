using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Vyuka.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string to, string subject, string html)
        {
            await SendAsync(to, subject, html, null);
        }

        public async Task SendAsync(
            string to,
            string subject,
            string html,
            List<EmailAttachment>? attachments)
        {
            var message = new MimeMessage();

            // FROM
            message.From.Add(new MailboxAddress(
                _config["Smtp:DisplayName"],
                _config["Smtp:From"]
            ));

            // TO
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = html
            };
            // ⭐ Přidání loga jako embedded obrázek
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.jpg");
            if (File.Exists(logoPath))
            {
                var logo = builder.LinkedResources.Add(logoPath);
                logo.ContentId = "logo";
                logo.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                logo.ContentType.MediaType = "image";
                logo.ContentType.MediaSubtype = "jpeg";
            }

            // ⭐ QR kódy jako embedded obrázky
            if (attachments != null)
            {
                foreach (var att in attachments)
                {
                    var img = builder.LinkedResources.Add(att.FilePath);
                    img.ContentId = att.ContentId;

                    // Gmail vyžaduje INLINE
                    img.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                    img.ContentType.Name = Path.GetFileName(att.FilePath);

                    // Typ souboru
                    if (att.FilePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        att.FilePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        img.ContentType.MediaType = "image";
                        img.ContentType.MediaSubtype = "jpeg";
                    }
                    else if (att.FilePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        img.ContentType.MediaType = "image";
                        img.ContentType.MediaSubtype = "png";
                    }
                }
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            // ⭐ Gmail – správné připojení (port 587 + STARTTLS)
            await client.ConnectAsync(
                _config["Smtp:Host"],
                int.Parse(_config["Smtp:Port"]),
                SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                _config["Smtp:User"],
                _config["Smtp:Password"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
