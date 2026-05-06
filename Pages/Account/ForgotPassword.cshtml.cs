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
            if (string.IsNullOrWhiteSpace(Email))
            {
                Message = "Zadejte email.";
                return Page();
            }

            // ✔ Najdeme uživatele přes Identity
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                Message = "Uživatel s tímto emailem neexistuje.";
                return Page();
            }

            // ✔ Vytvoříme vlastní token do tabulky PasswordResetTokens
            var token = Guid.NewGuid().ToString("N");

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,          // string
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // ✔ Odeslání emailu
            var resetLink = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword?token={token}";
            await _emailService.SendAsync(Email, "Reset hesla", $"Klikněte zde: {resetLink}");
            Message = "Pokyny pro reset hesla byly odeslány na váš email.";
            return Page();
        }
    }
}
