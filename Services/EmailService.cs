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

        // ⭐ Základní odeslání bez příloh
        public async Task SendAsync(string to, string subject, string html)
        {
            await SendAsync(to, subject, html, null);
        }

        // ⭐ Odeslání s přílohami (QR kódy, obrázky)
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

            // ⭐ Logo jako embedded obrázek
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.jpg");
            if (File.Exists(logoPath))
            {
                var logo = builder.LinkedResources.Add(logoPath);
                logo.ContentId = "logo";
                logo.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                logo.ContentType.MediaType = "image";
                logo.ContentType.MediaSubtype = "jpeg";
            }

            // ⭐ QR kódy / jiné obrázky jako embedded
            if (attachments != null)
            {
                foreach (var att in attachments)
                {
                    var img = builder.LinkedResources.Add(att.FilePath);
                    img.ContentId = att.ContentId;

                    img.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                    img.ContentType.Name = Path.GetFileName(att.FilePath);

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

            // ⭐ Gmail STARTTLS (port 587)
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

        // ⭐ Nová metoda pro reset hesla
        public async Task SendPasswordResetEmail(string email, string name, string token)
        {
            // URL aplikace z appsettings.json
            var baseUrl = _config["App:BaseUrl"] ?? "https://localhost:5001";

            var resetLink = $"{baseUrl}/Account/ResetPassword?token={token}";

            var subject = "Reset hesla";

            var html = $@"
                <p>Ahoj <strong>{name}</strong>,</p>
                <p>pro reset hesla klikni na následující odkaz:</p>
                <p><a href=""{resetLink}"">Resetovat heslo</a></p>
                <p>Odkaz je platný 1 hodinu.</p>
                <br>
                <img src=""cid:logo"" style=""width:150px;"" />
            ";

            await SendAsync(email, subject, html, null);
        }
    }
}
