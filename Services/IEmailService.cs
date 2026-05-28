using Vyuka.Models;

namespace Vyuka.Services
{
    public interface IEmailService
    {
        // ⭐ Základní odeslání
        Task SendAsync(string to, string subject, string html);

        // ⭐ Odeslání s přílohami
        Task SendAsync(string to, string subject, string html, List<EmailAttachment>? attachments);

        // ⭐ Odeslání s přílohami + dynamický QR + logování
        Task SendAsync(
            string to,
            string subject,
            string html,
            List<EmailAttachment>? attachments,
            decimal? dynamicAmount,
            string? dynamicMessage,
            string? customText,
            string? studentName,
            string emailType,
            int? studentId
        );

        // ⭐ Reset hesla
        Task SendPasswordResetEmail(string email, string name, string token);
    }
}
