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

            public double Credit { get; set; }

            public string? ParentName { get; set; }
            public string? ParentPhone { get; set; }
            public string? ParentEmail { get; set; }

            public string? LastLesson { get; set; }
            public string? NextLesson { get; set; }
            public DateTime? NextLessonDate { get; set; }

            public double PaidHours { get; set; }
            public double TaughtHours { get; set; }
            public double RemainingHours { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return RedirectToPage("/Index");

            // ⭐ Načteme studenty učitele
            var students = await _context.Students
                .Where(s => s.UserId == teacher.UserId)
                .Include(s => s.Subject)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // ⭐ Načteme všechny rodiče jedním dotazem
            var parents = await _context.Parents.ToListAsync();

            foreach (var s in students)
            {
                // ⭐ Najdeme rodiče pro daného studenta
                var parent = parents.FirstOrDefault(p => p.StudentId == s.Id);

                // ⭐ Zaplacené hodiny
                double paidHours = await _context.Payments
                    .Where(p => p.StudentId == s.Id)
                    .SumAsync(p => (double)p.HoursPurchased);

                // ⭐ Odučené hodiny – fallback pro staré lekce s 00:00–00:00
                var taughtLessons = await _context.Lessons
                    .Where(l => l.StudentId == s.Id)
                    .ToListAsync();

                double taughtHours = taughtLessons.Sum(l =>
                {
                    var duration = (l.End - l.Start).TotalHours;
                    return duration > 0 ? duration : 1.0; // ⭐ staré lekce bereme jako 1h
                });

                double remaining = paidHours - taughtHours;
                if (remaining < 0) remaining = 0;

                // ⭐ Poslední odučená lekce
                var lastLesson = taughtLessons
                    .OrderByDescending(l => l.Date)
                    .FirstOrDefault();

                // ⭐ Nejbližší plánovaná lekce
                var nextLesson = await _context.LessonPlans
                    .Where(lp => lp.StudentId == s.Id && lp.Date >= DateTime.Today)
                    .OrderBy(lp => lp.Date)
                    .FirstOrDefaultAsync();

                Students.Add(new StudentRow
                {
                    Id = s.Id,
                    FullName = $"{s.LastName} {s.FirstName}",
                    Subject = s.Subject?.Name ?? "",

                    Credit = Math.Round(remaining, 1),
                    PaidHours = Math.Round(paidHours, 1),
                    TaughtHours = Math.Round(taughtHours, 1),
                    RemainingHours = Math.Round(remaining, 1),

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
