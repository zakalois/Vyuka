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


        public void OnGet()
        {
            // Výpočet hodin v týdnu - od pondělí do neděle
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(7);

            // 1) Načteme lekce z databáze (SQL část)
            var lessonsThisWeek = _context.LessonPlans
                .Where(x => x.Date >= startOfWeek && x.Date < endOfWeek)
                .ToList(); // ← TADY je klíč – přepnutí na C# výpočet

            // 2) Spočítáme hodiny v C# (client-side)
            HoursThisWeek = lessonsThisWeek
                .Sum(x => (x.End - x.Start).TotalHours);



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
