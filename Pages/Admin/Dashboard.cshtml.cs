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

            // Načteme jen to, co Dashboard opravdu potřebuje
            var lessons = _context.LessonPlans
                .Select(x => new
                {
                    x.Date,
                    x.Start
                })
                .Where(x => x.Date >= DateTime.Today)
                .AsNoTracking()
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
        }
    }
}
