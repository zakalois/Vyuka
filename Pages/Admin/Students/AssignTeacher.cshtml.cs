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
        public int? SelectedTeacherId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public List<SelectListItem> Teachers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id, string? returnUrl)
        {
            StudentId = id;
            ReturnUrl = returnUrl ?? "/Admin/Students/Overview";

            var student = await _context.Students
                .Include(s => s.Teacher)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return RedirectToPage("Index");

            StudentName = $"{student.LastName} {student.FirstName}";

            Teachers = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "",
                    Text = "— Bez učitele —"
                }
            };

            Teachers.AddRange(
                await _context.Teachers
                    .Include(t => t.User)
                    .OrderBy(t => t.User.LastName)
                    .ThenBy(t => t.User.FirstName)
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = $"{t.User.LastName} {t.User.FirstName} ({t.User.Email})"
                    })
                    .ToListAsync()
            );

            SelectedTeacherId = student.TeacherId;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == StudentId);

            if (student == null)
                return RedirectToPage("Index");

            student.TeacherId = SelectedTeacherId;

            await _context.SaveChangesAsync();

            // ⭐ Návrat tam, odkud jsi přišel
            if (!string.IsNullOrWhiteSpace(ReturnUrl))
                return Redirect(ReturnUrl);

            return RedirectToPage("/Admin/Students/Overview");
        }
    }
}
