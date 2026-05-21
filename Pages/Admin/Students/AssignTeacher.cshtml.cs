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

        [BindProperty]
        public int StudentId { get; set; }

        public string StudentName { get; set; } = "";

        [BindProperty]
        public string? SelectedTeacherId { get; set; }

        public List<SelectListItem> Teachers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            StudentId = id;

            var student = await _context.Students
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return RedirectToPage("Index");

            // ⭐ OPRAVA – Student nemá FullName
            StudentName = $"{student.LastName} {student.FirstName}";

            Teachers = await _context.Teachers
    .Include(t => t.User)
    .OrderBy(t => t.User.LastName)
    .ThenBy(t => t.User.FirstName)
    .Select(t => new SelectListItem
    {
        Value = t.Id.ToString(), // ← TADY
        Text = $"{t.User.LastName} {t.User.FirstName}"
    })
    .ToListAsync();


            SelectedTeacherId = student.UserId;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == StudentId);

            if (student == null)
                return RedirectToPage("Index");

            student.UserId = SelectedTeacherId;

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Admin/Students/Overview", new { id = StudentId });
        }
    }
}
