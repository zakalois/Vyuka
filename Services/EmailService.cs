using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Vyuka.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string bodyHtml);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string bodyHtml)
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.UserName, "Výuka"),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };

            message.To.Add(to);

            await client.SendMailAsync(message);
        }
    }
}