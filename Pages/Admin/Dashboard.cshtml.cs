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

        // ENVIRONMENT INFO
        public string EnvironmentName { get; set; }
        public bool IsProduction => EnvironmentName == "Production";

        // NEXT LESSON
        public DateTime? NextLessonDate { get; set; }

        public void OnGet()
        {
            // 1) Zjistit prostředí
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

            // 2) Najít nejbližší lekci
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
