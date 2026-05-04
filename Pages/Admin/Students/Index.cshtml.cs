using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Student> Students { get; set; } = new List<Student>();

        // StudentId → Odučené hodiny
        public Dictionary<int, double> TaughtHours { get; set; } = new();

        public async Task OnGetAsync()
        {
            Students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // Načteme všechny odučené lekce
            var lessons = await _context.Lessons
                .Where(l => l.IsTaught)
                .ToListAsync();

            foreach (var s in Students)
            {
                double hours = lessons
                    .Where(l => l.StudentId == s.Id)
                    .Sum(l => (l.End - l.Start).TotalHours);

                TaughtHours[s.Id] = Math.Round(hours, 1);
            }
        }
    }
}
