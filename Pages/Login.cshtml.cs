using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Cryptography;
using System.Text;
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

        public List<SelectListItem> Users { get; set; }

        public string ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // Pokud už je přihlášený → pryč z loginu
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                return RedirectToPage("/Admin/Dashboard");
            }

            // Naplnění dropdownu z AppUsers
            Users = _context.AppUsers
                .Select(u => new SelectListItem
                {
                    Value = u.Email,
                    Text = $"{u.Name} ({u.Role})"
                })
                .ToList();

            return Page();
        }

        public IActionResult OnPost()
        {
            // Naplníme dropdown (musí být vždy)
            Users = _context.AppUsers
                .Select(u => new SelectListItem
                {
                    Value = u.Email,
                    Text = $"{u.Name} ({u.Role})"
                })
                .ToList();

            // Najdeme uživatele v AppUsers
            var user = _context.AppUsers.FirstOrDefault(u => u.Email == Email);

            if (user == null)
            {
                ErrorMessage = "Uživatel nenalezen.";
                return Page();
            }

            // Hash hesla
            string hashed = HashPassword(Password);

            if (hashed != user.PasswordHash)
            {
                ErrorMessage = "Nesprávné heslo.";
                return Page();
            }

            // Uložení do session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role);

            return RedirectToPage("/Admin/Dashboard");
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
