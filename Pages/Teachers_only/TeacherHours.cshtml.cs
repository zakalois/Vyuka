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

        // ⭐ nový filtr
        [BindProperty(SupportsGet = true)]
        public int? SelectedStudentId { get; set; }

        // ⭐ seznam studentů učitele
        public List<Student> Students { get; set; } = new();

        public List<Lesson> Results { get; set; } = new();
        public double TotalHours { get; set; }

        public async Task OnGetAsync()
        {
            // ⭐ výchozí datumy
            if (From == default || To == default)
            {
                var today = DateTime.Today;
                From = new DateTime(today.Year, today.Month, 1);
                To = today;
            }

            // ⭐ najdeme učitele
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return;

            // ⭐ načteme studenty učitele pro dropdown
            Students = await _context.Students
                .Where(s => s.TeacherId == teacher.Id)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // ⭐ základní dotaz na odučené hodiny
            var query = _context.Lessons
                .Include(l => l.Student)
                .Include(l => l.Subject)
                .Where(l =>
                    l.TeacherId == teacher.Id &&
                    l.IsTaught &&
                    l.Date >= From &&
                    l.Date <= To);

            // ⭐ pokud je vybraný student → filtrujeme
            if (SelectedStudentId.HasValue)
            {
                query = query.Where(l => l.StudentId == SelectedStudentId.Value);
            }

            // ⭐ výsledky
            Results = await query
                .OrderBy(l => l.Date)
                .ThenBy(l => l.Start)
                .ToListAsync();

            // ⭐ výpočet celkových hodin
            TotalHours = Results.Sum(l => (l.End - l.Start).TotalHours);
        }
    }
}
