using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Vyuka.Models;

namespace Vyuka.Pages.Teachers_only
{
    [Authorize(Roles = "Teacher")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public AppUser CurrentTeacher { get; set; }

        public List<LessonInfo> TodayLessons { get; set; } = new();
        public int StudentCount { get; set; }
        public int LowCreditCount { get; set; }
        public List<DaySchedule> WeeklySchedule { get; set; } = new();
        public List<string> Notifications { get; set; } = new();

        public async Task OnGet()
        {
            // 1️⃣ Přihlášený učitel
            CurrentTeacher = await _userManager.GetUserAsync(User);

            var teacher = await _context.Teachers
                .Include(t => t.Students)
                .FirstOrDefaultAsync(t => t.UserId == CurrentTeacher.Id);

            if (teacher == null)
                return;

            // 2️⃣ Dnešní hodiny
            var today = DateTime.Today;

            TodayLessons = await _context.Lessons
    .Include(l => l.Student)
    .Include(l => l.Subject)
    .Where(l => l.TeacherId == teacher.Id &&
                l.Date.Date == today)
    .OrderBy(l => l.Date)
    .Select(l => new LessonInfo(
        l.Date.ToString("HH:mm"),
        l.Student.FirstName + " " + l.Student.LastName,
        l.Subject.Name // ← TADY JE OPRAVA
    ))
    .ToListAsync();


            // 3️⃣ Počet studentů
            StudentCount = teacher.Students.Count;

            // 4️⃣ Studenti s nízkým kreditem
            LowCreditCount = teacher.Students.Count(s => s.RemainingHours <= 1);

            // 5️⃣ Týdenní rozvrh
            var weekLessons = await _context.Lessons
                .Where(l => l.TeacherId == teacher.Id &&
                            l.Date >= today &&
                            l.Date < today.AddDays(7))
                .ToListAsync(); // ← DŮLEŽITÉ!

            WeeklySchedule = weekLessons
                .GroupBy(l => l.Date.DayOfWeek)
                .Select(g => new DaySchedule(
                    GetCzechDayName(g.Key),
                    g.Count()
                ))
                .ToList();


            // 6️⃣ Upozornění
            Notifications = _context.Notes
    .Include(n => n.Student)
    .Where(n => n.TeacherId == teacher.Id)
    .OrderByDescending(n => n.Created)
    .Take(5)
    .Select(n => $"{n.Created:dd.MM.yyyy HH:mm} – {n.Student.FirstName} {n.Student.LastName}: {n.Text}")
    .ToList();


        }

        // Pomocná funkce pro české názvy dnů
        private string GetCzechDayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Pondělí",
                DayOfWeek.Tuesday => "Úterý",
                DayOfWeek.Wednesday => "Středa",
                DayOfWeek.Thursday => "Čtvrtek",
                DayOfWeek.Friday => "Pátek",
                DayOfWeek.Saturday => "Sobota",
                DayOfWeek.Sunday => "Neděle",
                _ => ""
            };
        }

        public record LessonInfo(string Time, string Student, string Subject);
        public record DaySchedule(string Day, int LessonCount);
    }
}
