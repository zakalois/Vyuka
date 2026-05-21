using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class ToggleActiveModel : PageModel
    {
        private readonly AppDbContext _context;

        public ToggleActiveModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return RedirectToPage("/Admin/Students/Overview");

            student.IsActive = !student.IsActive;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Admin/Students/Overview");
        }
    }
}
