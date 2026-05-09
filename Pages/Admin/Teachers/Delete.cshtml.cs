using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using Vyuka.Secrets;

namespace Vyuka.Pages.Admin.Teachers   // ← DŮLEŽITÉ!
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public DeleteModel(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public Teacher Teacher { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Teacher = await _db.Teachers.FindAsync(id);
            if (Teacher == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var teacher = await _db.Teachers.FindAsync(id);
            if (teacher == null)
                return NotFound();

            // Najdeme uživatele
            var user = await _userManager.FindByIdAsync(teacher.UserId);
            if (user != null)
            {
                // Odebereme roli Teacher
                await _userManager.RemoveFromRoleAsync(user, Roles.Teacher);
            }

            // Smažeme záznam učitele
            _db.Teachers.Remove(teacher);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Admin/Teachers/Teachers");
        }
    }
}
