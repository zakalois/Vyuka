using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _email;

        public ForgotPasswordModel(AppDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        [BindProperty]
        public string Email { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Email))
                return Page();

            // Najdeme uživatele v AppUsers
            var user = _context.AppUsers.FirstOrDefault(u => u.Email == Email);
            if (user == null)
                return Page(); // neprozrazujeme, že neexistuje

            // Vytvoříme token
            var token = Guid.NewGuid().ToString("N");

            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)   // ⭐ správný název vlastnosti
            });

            _context.SaveChanges();

            // Odešleme e‑mail (async → ale nemusí být awaited, protože vracíme redirect)
            _email.SendPasswordResetEmail(user.Email, user.Name, token);

            return RedirectToPage("/Account/ForgotPasswordConfirmation");
        }
    }
}
