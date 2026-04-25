using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vyuka.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // 🔥 Role ze session, ne z claims
            var role = HttpContext.Session.GetString("UserRole");

            if (role == null)
                return RedirectToPage("/Login");

            return role switch
            {
                "Admin" => RedirectToPage("/Dashboard/Admin"),
                "Teacher" => RedirectToPage("/Dashboard/Teacher"),
                "Student" => RedirectToPage("/Dashboard/Student"),
                _ => RedirectToPage("/Login")
            };
        }
    }
}
