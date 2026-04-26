namespace Vyuka.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string html);

        // 🔵 Nová verze s přílohami (QR kódy)
        Task SendAsync(string to, string subject, string html, List<EmailAttachment>? attachments);
    }

}