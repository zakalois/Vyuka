using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Teachers
{
    public class TeachersModel : PageModel
    {
        private readonly AppDbContext _context;

        public TeachersModel(AppDbContext context)
        {
            _context = context;
        }

        public List<TeacherListViewModel> Teachers { get; set; }

        public async Task OnGetAsync()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            Teachers = await _context.Teachers
                .Include(t => t.User)
                .Select(t => new TeacherListViewModel
                {
                    Id = t.Id,
                    FirstName = t.User.FirstName,
                    LastName = t.User.LastName,
                    FullName = t.User.FirstName + " " + t.User.LastName,
                    Email = t.User.Email,
                    Phone = t.User.PhoneNumber,
                    Subjects = "",

                    // Počet studentů
                    StudentsCount = _context.Students
                        .Count(s => s.TeacherId == t.Id),

                    // ⭐ Hodiny tento měsíc – používáme Lesson.Hours (decimal)
                    HoursThisMonth = (int)_context.Lessons
    .Where(l =>
        l.TeacherId == t.Id &&
        l.Date >= monthStart &&
        l.Date < monthEnd &&
        l.IsTaught == true
    )
    .Sum(l => l.Hours),


                    IsActive = t.IsActive
                })
                .ToListAsync();
        }
    }
}
