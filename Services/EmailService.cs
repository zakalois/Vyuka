using System.Net;
using System.Net.Mail;

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
            try
            {
                var smtp = new SmtpClient
                {
                    Host = _config["Smtp:Host"],
                    Port = int.Parse(_config["Smtp:Port"]),
                    EnableSsl = true,
                    Credentials = new NetworkCredential(
                        _config["Smtp:User"],
                        _config["Smtp:Password"]
                    )
                };

                var msg = new MailMessage
                {
                    From = new MailAddress(
                         _config["Smtp:From"],
                         _config["Smtp:DisplayName"] ?? "Výuka systém"
),
                    Subject = subject,
                    Body = html,
                    IsBodyHtml = true
                };

                msg.To.Add(to);
                // Skrytá kopie na adresu zakalois@ucitelzak.eu
                msg.Bcc.Add("zakalois@ucitelzak.eu");

                await smtp.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SMTP ERROR: " + ex.Message);
                throw;
            }
        }
    }
}