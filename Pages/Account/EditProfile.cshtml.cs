using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Account
{
    public class EditProfileModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditProfileModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AppUser Input { get; set; }

        public IActionResult OnGet()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null)
                return RedirectToPage("/Login");

            var user = _context.AppUsers.FirstOrDefault(u => u.Id == sessionUserId.Value);
            if (user == null)
                return RedirectToPage("/Login");

            Input = new AppUser
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            };

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

            user.Name = Input.Name;
            user.Email = Input.Email;
            // Role se nemění

            _context.SaveChanges();

            return RedirectToPage("/Account/Profile");
        }
    }
}
