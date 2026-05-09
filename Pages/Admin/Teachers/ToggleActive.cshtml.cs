using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Teachers
{
    public class ToggleActiveModel : PageModel
    {
        private readonly AppDbContext _db;

        public ToggleActiveModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var teacher = await _db.Teachers.FindAsync(id);
            if (teacher == null)
                return NotFound();

            teacher.IsActive = !teacher.IsActive;
            await _db.SaveChangesAsync();

            return RedirectToPage("/Admin/Teachers/Teachers");
        }
    }
}
