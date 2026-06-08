using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vyuka.Models;

namespace Vyuka.Pages.Teachers_only
{
    public class TeacherHoursModel : PageModel
    {
        private readonly AppDbContext _context;

        public TeacherHoursModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime From { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime To { get; set; }

        public List<Lesson> Results { get; set; } = new();
        public double TotalHours { get; set; }

        public async Task OnGetAsync()
        {
            if (From == default || To == default)
            {
                // 🔥 Výchozí hodnoty
                var today = DateTime.Today;
                From = new DateTime(today.Year, today.Month, 1); // začátek měsíce
                To = today;                                      // dnešek
            }


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return;

            Results = await _context.Lessons
                .Include(l => l.Student)
                .Include(l => l.Subject)
                .Where(l =>
                    l.TeacherId == teacher.Id &&
                    l.IsTaught == true &&
                    l.Date >= From &&
                    l.Date <= To)
                .OrderBy(l => l.Date)
                .ThenBy(l => l.Start)
                .ToListAsync();

            TotalHours = Results.Sum(l => (l.End - l.Start).TotalHours);
        }
    }
}
