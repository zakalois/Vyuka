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

        public EmailService(IOptions<SmtpSettings> smtp)
        {
            _smtp = smtp.Value;
        }

        // ⭐ 1) Povinná metoda bez příloh
        public async Task SendAsync(string to, string subject, string html)
        {
            await SendAsync(to, subject, html, null);
        }

        // ⭐ 2) Povinná metoda s přílohami
        public async Task SendAsync(string to, string subject, string html, List<EmailAttachment>? attachments)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Výuka App", _smtp.From));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = html
            };

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

        // ⭐ 3) Reset hesla
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
