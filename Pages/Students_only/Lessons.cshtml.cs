using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Students_only
{
    public class LessonsModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public LessonsModel(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public string StudentName { get; set; } = "";

        public decimal PrepaidHours { get; set; }
        public decimal TaughtHours { get; set; }
        public decimal Balance => PrepaidHours - TaughtHours;

        public LessonRow? NextLesson { get; set; }
        public List<LessonRow> Lessons { get; set; } = new();

        public class LessonRow
        {
            public DateTime Date { get; set; }
            public string Subject { get; set; } = "";
            public string Topic { get; set; } = "";
            public decimal Hours { get; set; }
            public string Type { get; set; } = "";
        }

        public async Task OnGetAsync()
        {
            // 1) Přihlášený Identity uživatel
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return;

            // 2) Najdeme studenta podle emailu (bez migrací)
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.Email == user.Email);

            if (student == null)
                return;

            int studentId = student.Id;
            StudentName = student.FullName;

            // 3) Výpočty
            PrepaidHours = Math.Round(
                await _db.Payments
                    .Where(p => p.StudentId == studentId)
                    .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0, 1);

            TaughtHours = Math.Round(
                await _db.Lessons
                    .Where(l => l.StudentId == studentId)
                    .SumAsync(l => (decimal?)l.Hours) ?? 0, 1);

            // 4) Nejbližší plánovaná hodina
            var next = await _db.LessonPlans
                .Include(l => l.Subject)
                .Include(l => l.SubjectTopic)
                .Where(l => l.StudentId == studentId && l.Date > DateTime.Now)
                .OrderBy(l => l.Date)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                NextLesson = new LessonRow
                {
                    Date = next.Date,
                    Subject = next.Subject.Name,
                    Topic = next.SubjectTopic?.Name ?? "",
                    Hours = Math.Round((decimal)(next.End - next.Start).TotalHours, 1),
                    Type = "Plánovaná"
                };
            }

            // 5) Plánované
            var planned = await _db.LessonPlans
                .Include(l => l.Subject)
                .Include(l => l.SubjectTopic)
                .Where(l => l.StudentId == studentId)
                .ToListAsync();

            var plannedRows = planned.Select(l => new LessonRow
            {
                Date = l.Date,
                Subject = l.Subject.Name,
                Topic = l.SubjectTopic?.Name ?? "",
                Hours = Math.Round((decimal)(l.End - l.Start).TotalHours, 1),
                Type = "Plánovaná"
            });

            // 6) Odučené
            var taught = await _db.Lessons
                .Include(l => l.Subject)
                .Include(l => l.SubjectTopic)
                .Where(l => l.StudentId == studentId)
                .ToListAsync();

            var taughtRows = taught.Select(l => new LessonRow
            {
                Date = l.Date,
                Subject = l.Subject.Name,
                Topic = l.SubjectTopic?.Name ?? "",
                Hours = Math.Round(l.Hours, 1),
                Type = "Odučená"
            });

            // 7) Spojení
            Lessons = plannedRows
                .Concat(taughtRows)
                .OrderByDescending(l => l.Date)
                .ToList();
        }
    }
}
