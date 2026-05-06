using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using Microsoft.AspNetCore.Identity;

namespace Vyuka.Pages.Admin.Teachers
{
    public class TeacherDetailModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public TeacherDetailModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public AppUser Teacher { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
                return NotFound();

            Teacher = await _userManager.FindByIdAsync(id);

            if (Teacher == null)
                return NotFound();

            return Page();
        }
    }
}
