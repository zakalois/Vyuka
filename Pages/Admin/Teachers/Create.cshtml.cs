using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using Microsoft.AspNetCore.Identity;
using Vyuka.Services;

namespace Vyuka.Pages.Admin.Teachers
{
    public class CreateModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public CreateModel(
            UserManager<AppUser> userManager,
            AppDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = new AppUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                PhoneNumber = Input.Phone,
                Role = "Teacher"
            };

            // 🔐 Vygenerujeme dočasné heslo (nebude použito)
            string tempPassword = GeneratePassword();

            // vytvoření identity účtu
            var result = await _userManager.CreateAsync(user, tempPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return Page();
            }

            // ⭐ 1) Vytvoříme token pro nastavení hesla
            var token = Guid.NewGuid().ToString("N");

            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            await _context.SaveChangesAsync();

            // ⭐ 2) Odešleme e‑mail s odkazem
            await _emailService.SendPasswordResetEmail(
                user.Email,
                user.FirstName,
                token
            );

            return RedirectToPage("/Admin/Teachers/Teachers");
        }

        // 🔽 GENERÁTOR HESLA
        private string GeneratePassword()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";

            var rand = new Random();

            string password =
                upper[rand.Next(upper.Length)].ToString() +
                lower[rand.Next(lower.Length)].ToString() +
                digits[rand.Next(digits.Length)].ToString() +
                special[rand.Next(special.Length)].ToString();

            string all = upper + lower + digits + special;
            while (password.Length < 10)
                password += all[rand.Next(all.Length)];

            return password;
        }
    }
}
