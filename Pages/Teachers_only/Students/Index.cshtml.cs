using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Teachers_only.Students
{
    [Authorize(Roles = "Teacher")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<StudentOverviewDto> Students { get; set; } = new();

        public async Task OnGet()
        {
            // 1) Najdeme přihlášeného učitele
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (teacher == null)
            {
                Students = new();
                return;
            }

            // 2) Načteme studenty učitele
            var students = await _context.Students
                .Where(s => s.TeacherId == teacher.Id)
                .Include(s => s.Subject)
                .Include(s => s.Lessons)
                .ToListAsync();

            // 3) Převedeme na DTO pro přehled
            Students = students.Select(s => new StudentOverviewDto
            {
                Id = s.Id,
                FullName = $"{s.LastName} {s.FirstName}",
                Subject = s.Subject?.Name ?? "-",
                Credit = Convert.ToInt32(
    _context.Payments
        .Where(p => p.StudentId == s.Id)
        .Sum(p => p.HoursPurchased)
    -
    s.Lessons
        .Where(l => l.IsTaught)
        .Sum(l => l.Hours)
),


                ParentName = $"{s.ParentFirstName} {s.ParentLastName}".Trim(),
                ParentPhone = s.ParentPhone,
                ParentEmail = s.ParentEmail,

                // ⭐ POSLEDNÍ ODUČENÁ LEKCE
                LastLesson = s.Lessons
                    .Where(l => l.IsTaught)
                    .OrderByDescending(l => l.Date)
                    .ThenByDescending(l => l.Start)
                    .Select(l => (l.Date + l.Start).ToString("dd.MM.yyyy HH:mm"))
                    .FirstOrDefault() ?? "-",

                // ⭐ NEJBLIŽŠÍ PLÁNOVANÁ LEKCE
                NextLesson = s.Lessons
                    .Where(l => !l.IsTaught && l.Date >= DateTime.Today)
                    .OrderBy(l => l.Date)
                    .ThenBy(l => l.Start)
                    .Select(l => (l.Date + l.Start).ToString("dd.MM.yyyy HH:mm"))
                    .FirstOrDefault() ?? "-",

                // ⭐ DATUM DALŠÍ LEKCE (pro barvy v tabulce)
                NextLessonDate = s.Lessons
                    .Where(l => !l.IsTaught && l.Date >= DateTime.Today)
                    .OrderBy(l => l.Date)
                    .ThenBy(l => l.Start)
                    .Select(l => l.Date)
                    .FirstOrDefault()
            })
            .OrderBy(s => s.FullName)
            .ToList();
        }

        public class StudentOverviewDto
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Subject { get; set; }
            public int Credit { get; set; }

            public string LastLesson { get; set; }
            public string NextLesson { get; set; }
            public DateTime NextLessonDate { get; set; }

            public string ParentName { get; set; }
            public string ParentPhone { get; set; }
            public string ParentEmail { get; set; }
        }
    }
}
