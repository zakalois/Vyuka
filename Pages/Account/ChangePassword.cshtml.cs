using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text;
using Vyuka.Models;

namespace Vyuka.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly AppDbContext _context;

        public ChangePasswordModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ChangePasswordInput Input { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public class ChangePasswordInput
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToPage("/Login");

            return Page();
        }

        public IActionResult OnPost()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null)
                return RedirectToPage("/Login");

            var user = _context.AppUsers.FirstOrDefault(u => u.Id == sessionUserId.Value);
            if (user == null)
                return RedirectToPage("/Login");

            // 1) Ověření starého hesla
            var oldHash = HashPassword(Input.OldPassword);
            if (oldHash != user.PasswordHash)
            {
                ErrorMessage = "Současné heslo není správné.";
                return Page();
            }

            // 2) Ověření nového hesla
            if (Input.NewPassword != Input.ConfirmPassword)
            {
                ErrorMessage = "Nové heslo a potvrzení hesla se neshodují.";
                return Page();
            }

            // 3) Uložení nového hesla
            user.PasswordHash = HashPassword(Input.NewPassword);
            _context.SaveChanges();

            SuccessMessage = "Heslo bylo úspěšně změněno.";
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
