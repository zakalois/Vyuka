using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public DateTime? NextLessonDate { get; set; }

        // ⭐ Přidané vlastnosti
        public int EmailsLast30Days { get; set; }
        public int EmailErrorsLast30Days { get; set; }
        public double HoursThisWeek { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int InactiveStudents { get; set; }



        public void OnGet()
        {
            var today = DateTime.Today;

            // Správný výpočet pondělí i v neděli
            int offset = (int)today.DayOfWeek == 0
                ? -6
                : (int)DayOfWeek.Monday - (int)today.DayOfWeek;

            var startOfWeek = today.AddDays(offset);
            var endOfWeek = startOfWeek.AddDays(7);

            // Načtení lekcí
            var lessonsThisWeek = _context.Lessons
                .Where(l => l.IsTaught == true &&
                            l.Date >= startOfWeek &&
                            l.Date < endOfWeek)
                .ToList();

            // Výpočet hodin přesně jako SQL
            HoursThisWeek = lessonsThisWeek
               .Sum(l => l.End > l.Start
    ? Math.Round((l.End - l.Start).TotalMinutes / 60.0, 1)
    : (double)l.Hours);


            // Výpočet studentů
            TotalStudents = _context.Students.Count();
            ActiveStudents = _context.Students.Count(s => s.IsActive);
            InactiveStudents = _context.Students.Count(s => !s.IsActive);

            // původní kód pro NextLessonDate
            var lessons = _context.LessonPlans
                .Where(x => x.Date >= DateTime.Today)
                .ToList();

            NextLessonDate = lessons
                .Select(x => new DateTime(
                    x.Date.Year,
                    x.Date.Month,
                    x.Date.Day,
                    x.Start.Hours,
                    x.Start.Minutes,
                    x.Start.Seconds
                ))
                .Where(dt => dt > DateTime.Now)
                .OrderBy(dt => dt)
                .FirstOrDefault();

            // ⭐ Nové statistiky e‑mailů
            var since = DateTime.Now.AddDays(-30);

            EmailsLast30Days = _context.EmailLogs
                .Count(l => l.SentAt >= since);

            EmailErrorsLast30Days = _context.EmailLogs
                .Count(l => l.SentAt >= since && !l.Success);
        }
    }
}
