using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Student Student { get; set; }

        public List<SelectListItem> Subjects { get; set; }

        private async Task LoadSubjectsAsync()
        {
            Subjects = await _context.Subjects
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Student = await _context.Students.FindAsync(id);

            if (Student == null)
                return RedirectToPage("/Admin/Students/Overview");

            await LoadSubjectsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadSubjectsAsync(); // ← MUSÍ BÝT I TADY

            if (!ModelState.IsValid)
                return Page();

            _context.Attach(Student).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToPage("/Admin/Students/Overview");
        }
    }
}
