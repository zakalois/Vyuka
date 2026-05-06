using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vyplňte email i heslo.";
                return Page();
            }

            // ✔ Najdeme uživatele v AspNetUsers
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Email);

            if (user == null)
            {
                ErrorMessage = "Nesprávný email nebo heslo.";
                return Page();
            }

            // ✔ Ověření hesla (Identity hash)
            var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<AppUser>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Password);

            if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                ErrorMessage = "Nesprávný email nebo heslo.";
                return Page();
            }

            // ✔ Uložíme session
            HttpContext.Session.SetString("UserId", user.Id);

            // ✔ Redirect podle role
            if (user.Role == "Admin")
                return RedirectToPage("/Admin/Dashboard");

            if (user.Role == "Teacher")
                return RedirectToPage("/Teacher/Dashboard");

            return RedirectToPage("/Student/Dashboard");
        }
    }
}
