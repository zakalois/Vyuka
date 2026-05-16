using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public bool IsProduction { get; set; }
        public DateTime? NextLessonDate { get; set; }

        public void OnGet()
        {
#if DEBUG
            IsProduction = false;
#else
            IsProduction = true;
#endif

            // ⭐ Načítáme bezpečně – včetně navigačních vlastností
            var lessons = _context.LessonPlans
                .Include(x => x.Student)
                .Include(x => x.Subject)
                .Include(x => x.SubjectTopic)
                .Include(x => x.Teacher)   // ⭐ nově přidané
                .Where(x => x.Date >= DateTime.Today)
                .ToList();

            // ⭐ Výpočet nejbližší lekce – bezpečný
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
        }
    }
}
