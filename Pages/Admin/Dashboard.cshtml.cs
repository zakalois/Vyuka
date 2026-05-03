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

        public DateTime? NextLessonDate { get; set; }

        public void OnGet()
        {
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
