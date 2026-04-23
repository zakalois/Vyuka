namespace Vyuka.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string html);
    }
}