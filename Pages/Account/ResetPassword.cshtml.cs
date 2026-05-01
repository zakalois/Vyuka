using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text;
using Vyuka.Models;

namespace Vyuka.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly AppDbContext _context;

        public ResetPasswordModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string NewPassword { get; set; }

        public string Message { get; set; }

        public IActionResult OnGet(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Message = "Neplatný odkaz.";
                return Page();
            }

            var reset = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == token);

            // ⭐ Opraveno: ExpiresAt místo Expiration
            if (reset == null || reset.ExpiresAt < DateTime.UtcNow)
            {
                Message = "Odkaz je neplatný nebo expiroval.";
                return Page();
            }

            // token je platný → uložíme si ho do TempData
            TempData["token"] = token;

            return Page();
        }

        public IActionResult OnPost()
        {
            var token = TempData["token"]?.ToString();

            if (string.IsNullOrWhiteSpace(token))
            {
                Message = "Token chybí.";
                return Page();
            }

            var reset = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == token);

            // ⭐ Opraveno: ExpiresAt místo Expiration
            if (reset == null || reset.ExpiresAt < DateTime.UtcNow)
            {
                Message = "Odkaz je neplatný nebo expiroval.";
                return Page();
            }

            var user = _context.AppUsers.FirstOrDefault(u => u.Id == reset.UserId);

            if (user == null)
            {
                Message = "Uživatel nenalezen.";
                return Page();
            }

            // Hash nového hesla
            user.PasswordHash = HashPassword(NewPassword);

            // Token smažeme
            _context.PasswordResetTokens.Remove(reset);

            _context.SaveChanges();

            Message = "Heslo bylo úspěšně změněno.";
            return Page();
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
