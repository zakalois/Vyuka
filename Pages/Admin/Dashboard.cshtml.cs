using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using System.Linq;

namespace Vyuka.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        // PRODUKCE / VÝVOJ
        public bool IsProduction { get; set; }

        // NEJBLIŽŠÍ LEKCE
        public DateTime? NextLessonDate { get; set; }

        public void OnGet()
        {
            // ⭐ ROZLIŠENÍ PRODUKCE / VÝVOJE
#if DEBUG
            IsProduction = false;   // vývojová verze
#else
            IsProduction = true;    // produkční verze
#endif

            // ⭐ NAČTENÍ NEJBLIŽŠÍ LEKCE
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
        }
    }
}
