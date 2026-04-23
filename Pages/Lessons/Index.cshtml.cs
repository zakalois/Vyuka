using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Lessons
{
    public class LessonsIndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public LessonsIndexModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public SelectList StudentList { get; set; } = default!;
        public List<Lesson> Lessons { get; set; } = new();

        public decimal TotalTaughtHours { get; set; }

        [BindProperty(SupportsGet = true)]
        public int SelectedStudentId { get; set; }

        public async Task OnGetAsync()
        {
            // Načteme studenty – třídění podle příjmení a pak jména
            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // Dropdown: PŘÍJMENÍ + JMÉNO (používá FullName z modelu)
            StudentList = new SelectList(students, "Id", "FullName");

            if (SelectedStudentId > 0)
            {
                var query = _context.Lessons
                    .Include(l => l.Subject)
                    .Include(l => l.SubjectTopic)
                    .Where(l => l.StudentId == SelectedStudentId && l.IsTaught)
                    .AsQueryable();

                // Filtr od data
                if (FromDate.HasValue)
                    query = query.Where(l => l.Date >= FromDate.Value);

                // Filtr do data
                if (ToDate.HasValue)
                    query = query.Where(l => l.Date <= ToDate.Value);

                Lessons = await query
                    .OrderByDescending(l => l.Date)
                    .ToListAsync();

                TotalTaughtHours = Lessons.Sum(l => l.Hours);
            }
        }
    }
}