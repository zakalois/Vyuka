namespace Vyuka.Services
{
    public interface IEmailService
    {
        // ⭐ Základní odeslání
        Task SendAsync(string to, string subject, string html);

        // ⭐ Odeslání s přílohami (QR kódy, obrázky)
        Task SendAsync(string to, string subject, string html, List<EmailAttachment>? attachments);

        // ⭐ Nová metoda pro reset hesla
        Task SendPasswordResetEmail(string email, string name, string token);
    }
}
