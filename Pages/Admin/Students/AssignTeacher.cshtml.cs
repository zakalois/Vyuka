using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class AssignTeacherModel : PageModel
    {
        private readonly AppDbContext _context;

        public AssignTeacherModel(AppDbContext context)
        {
            _context = context;
        }

        // ID studenta z URL
        [BindProperty]
        public int StudentId { get; set; }

        // Jméno studenta pro zobrazení
        public string StudentName { get; set; } = "";

        // Vybraný učitel (int?)
        [BindProperty]
        public int? SelectedTeacherId { get; set; }

        // Seznam učitelů pro dropdown
        public List<SelectListItem> Teachers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            StudentId = id;

            var student = await _context.Students
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return RedirectToPage("Index");

            StudentName = student.FullName;

            // načteme učitele
            Teachers = await _context.Teachers
                .Include(t => t.User)
                .OrderBy(t => t.User.LastName)
                .ThenBy(t => t.User.FirstName)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.User.LastName} {t.User.FirstName}"
                })
                .ToListAsync();


            // předvyplníme aktuálního učitele
            SelectedTeacherId = student.TeacherId;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == StudentId);

            if (student == null)
                return RedirectToPage("Index");

            // přiřazení učitele
            student.TeacherId = SelectedTeacherId;

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}
