using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public ForgotPasswordModel(
            UserManager<AppUser> userManager,
            AppDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public string Email { get; set; }

        public string Message { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            // ⭐ 1) Validace – email musí být zadán
            if (string.IsNullOrWhiteSpace(Email))
            {
                Message = "Zadejte email.";
                return Page();
            }

            // ⭐ 2) Najdeme uživatele
            var user = await _userManager.FindByEmailAsync(Email);

            // ⭐ 3) Bezpečnost – NEprozrazujeme, zda existuje
            if (user == null)
            {
                Message = "Pokud účet existuje, poslali jsme vám instrukce na email.";
                return Page();
            }

            // ⭐ 4) Smažeme staré tokeny
            var oldTokens = _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id);

            _context.PasswordResetTokens.RemoveRange(oldTokens);

            // ⭐ 5) Vytvoříme nový token
            var token = Guid.NewGuid().ToString("N");

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // ⭐ 6) Odeslání emailu – pouze pokud user existuje
            await _emailService.SendPasswordResetEmail(
                user.Email,
                user.FirstName ?? "",
                token
            );

            // ⭐ 7) Stejná zpráva pro existující i neexistující email
            Message = "Pokud účet existuje, poslali jsme vám instrukce na email.";
            return Page();
        }
    }
}
