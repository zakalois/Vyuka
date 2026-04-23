using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Students
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Student Student { get; set; } = default!;
        public List<Subject> Subjects { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // ⭐ Načteme studenta
            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (Student == null)
                return NotFound();

            // ⭐ Načteme předměty studenta
            Subjects = await _context.StudentSubjects
                .Where(ss => ss.StudentId == id)
                .Select(ss => ss.Subject)
                .ToListAsync();

            return Page();
        }
    }
}
