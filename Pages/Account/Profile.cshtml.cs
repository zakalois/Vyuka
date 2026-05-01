using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Account
{
    public class ProfileModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProfileModel(AppDbContext context)
        {
            _context = context;
        }

        // ⭐ Jediný správný typ
        public AppUser UserData { get; set; }

        public IActionResult OnGet()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null)
                return RedirectToPage("/Login");

            // ⭐ Používáme AppUsers (správná tabulka)
            UserData = _context.AppUsers.FirstOrDefault(u => u.Id == sessionUserId.Value);

            if (UserData == null)
                return RedirectToPage("/Login");

            return Page();
        }
    }
}
