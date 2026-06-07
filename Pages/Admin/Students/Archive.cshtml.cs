using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class ArchiveModel : PageModel
    {
        private readonly AppDbContext _context;

        public ArchiveModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Student CurrentStudent { get; set; } = new();

        [BindProperty]
        public string ArchiveReason { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int id)
        {
            CurrentStudent = await _context.Students.FindAsync(id);

            if (CurrentStudent == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var student = await _context.Students.FindAsync(CurrentStudent.Id);

            if (student == null)
                return NotFound();

            student.IsActive = false;
            student.ArchivedAt = DateTime.Now;
            student.ArchiveReason = ArchiveReason;

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            return RedirectToPage("Overview");
        }
    }
}
