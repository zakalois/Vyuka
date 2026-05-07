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
            Console.WriteLine("EMAIL: SendAsync spuštěno");

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _config["Smtp:DisplayName"],
                _config["Smtp:From"]
            ));

            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = html
            };

            // ⭐ Logo jako embedded obrázek – OPRAVENO
            if (html.Contains("cid:logo"))
            {
                var logoPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "images",
                    "users",
                    "logo.jpg"
                );

                Console.WriteLine("EMAIL: Logo path = " + logoPath);

                if (File.Exists(logoPath))
                {
                    Console.WriteLine("EMAIL: Logo existuje");
                    var logo = builder.LinkedResources.Add(logoPath);
                    logo.ContentId = "logo";
                    logo.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                    logo.ContentType.MediaType = "image";
                    logo.ContentType.MediaSubtype = "jpeg";
                }
                else
                {
                    Console.WriteLine("EMAIL: Logo NEEXISTUJE");
                }
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

            try
            {
                Console.WriteLine("SMTP: Připojuji se...");

                await client.ConnectAsync(
                    _config["Smtp:Host"],
                    int.Parse(_config["Smtp:Port"]),
                    SecureSocketOptions.StartTls
                );

                Console.WriteLine("SMTP: Autentizace...");

                await client.AuthenticateAsync(
                    _config["Smtp:User"],
                    _config["Smtp:Password"]
                );

                Console.WriteLine("SMTP: Odesílám...");

                await client.SendAsync(message);

                Console.WriteLine("SMTP: Hotovo.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("SMTP ERROR: " + ex.Message);
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        // ⭐ Nová metoda pro reset hesla
        public async Task SendPasswordResetEmail(string email, string name, string token)
        {
            Console.WriteLine("RESET EMAIL: metoda byla zavolána");

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
