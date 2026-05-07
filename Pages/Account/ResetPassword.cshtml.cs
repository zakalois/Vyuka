using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Vyuka.Models;

namespace Vyuka.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPasswordHasher<AppUser> _passwordHasher;

        public ResetPasswordModel(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IPasswordHasher<AppUser> passwordHasher)
        {
            _context = context;
            _userManager = userManager;
            _passwordHasher = passwordHasher;
        }

        [BindProperty] public string Token { get; set; }
        [BindProperty] public string NewPassword { get; set; }
        [BindProperty] public string ConfirmPassword { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public IActionResult OnGet(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToPage("/Login");

            Token = token;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Hesla se neshodují.";
                return Page();
            }

            // ✔ Najdeme token v naší tabulce
            var resetToken = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == Token);
            if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
            {
                ErrorMessage = "Token je neplatný nebo vypršel.";
                return Page();
            }

            // ✔ Najdeme uživatele
            var user = await _userManager.FindByIdAsync(resetToken.UserId);
            if (user == null)
            {
                ErrorMessage = "Uživatel nenalezen.";
                return Page();
            }

            // ⭐ OPRAVA: Reset hesla ručně přes PasswordHasher
            user.PasswordHash = _passwordHasher.HashPassword(user, NewPassword);

            // ✔ Uložíme změnu
            _context.PasswordResetTokens.Remove(resetToken);
            await _context.SaveChangesAsync();

            SuccessMessage = "Heslo bylo úspěšně změněno.";
            return Page();
        }
    }
}
