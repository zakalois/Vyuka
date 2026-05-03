using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students

{
    public class ToggleModel : PageModel
    {
        private readonly AppDbContext _context;

        public ToggleModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
                return NotFound();

            student.IsActive = !student.IsActive; // přepnutí stavu

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}