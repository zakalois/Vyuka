using Vyuka.Models;

namespace Vyuka.Services
{
    public interface IEmailService
    {
        // ⭐ Základní odeslání
        Task SendAsync(string to, string subject, string html);

        // ⭐ Odeslání s přílohami
        Task SendAsync(string to, string subject, string html, List<EmailAttachment>? attachments);

        // ⭐ Odeslání s přílohami + dynamický QR kód
        Task SendAsync(
            string to,
            string subject,
            string html,
            List<EmailAttachment>? attachments,
            decimal? dynamicAmount,
            string? dynamicMessage);

        // ⭐ Reset hesla
        Task SendPasswordResetEmail(string email, string name, string token);
    }
}
