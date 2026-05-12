using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vyuka.Models;

namespace Vyuka.Pages.Teachers_only.Students
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public List<StudentRow> Students { get; set; } = new();

        public class StudentRow
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Subject { get; set; }
            public int Credit { get; set; }
            public string? ParentName { get; set; }
            public string? ParentPhone { get; set; }
            public string? ParentEmail { get; set; }
            public string? LastLesson { get; set; }
            public string? NextLesson { get; set; }
            public DateTime? NextLessonDate { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return RedirectToPage("/Index");

            // ⭐ Načteme studenty BEZ Parent
            var students = await _context.Students
                .Where(s => s.TeacherId == teacher.Id)
                .Include(s => s.Subject)
                .Include(s => s.Lessons)
                .Include(s => s.LessonPlans)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // ⭐ Načteme rodiče zvlášť (FK: Parent.StudentId)
            var parents = await _context.Parents.ToListAsync();

            foreach (var s in students)
            {
                var parent = parents.FirstOrDefault(p => p.StudentId == s.Id);

                var lastLesson = s.Lessons
                    .OrderByDescending(l => l.Date)
                    .FirstOrDefault();

                var nextLesson = s.LessonPlans
                    .Where(lp => lp.Date >= DateTime.Today)
                    .OrderBy(lp => lp.Date)
                    .FirstOrDefault();

                int taughtHours = s.Lessons.Count(l => l.IsTaught);
                int remainingCredit = s.Credit - taughtHours;

                Students.Add(new StudentRow
                {
                    Id = s.Id,
                    FullName = $"{s.LastName} {s.FirstName}",
                    Subject = s.Subject?.Name ?? "",
                    Credit = remainingCredit,

                    ParentName = parent?.Name,
                    ParentPhone = parent?.Phone,
                    ParentEmail = parent?.Email,

                    LastLesson = lastLesson != null
                        ? $"{lastLesson.Date:dd.MM.yyyy} {lastLesson.Start:hh\\:mm}"
                        : "-",

                    NextLesson = nextLesson != null
                        ? $"{nextLesson.Date:dd.MM.yyyy} {nextLesson.Start:hh\\:mm}"
                        : "-",

                    NextLessonDate = nextLesson?.Date
                });
            }

            return Page();
        }
    }
}
