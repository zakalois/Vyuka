using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

            // 2️⃣ Správné datum v českém časovém pásmu
            var today = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time")
            ).Date;

            // 3️⃣ Dnešní hodiny – čistá logika jako admin
            var todayLessonsRaw = await _context.Lessons
                .Include(l => l.Student)
                .Include(l => l.Subject)
                .Include(l => l.SubjectTopic)
              .Where(l =>
    l.TeacherId == teacher.Id &&
    l.Date >= today &&
    l.Date < today.AddDays(1)
)

                .OrderBy(l => l.Start)
                .ToListAsync();

            TodayLessons = todayLessonsRaw
                .Select(l => new LessonInfo(
                    l.Start.ToString(@"hh\:mm"),
                    $"{l.Student.FirstName} {l.Student.LastName}",
                    l.Subject.Name
                ))
                .ToList();

            // 4️⃣ Počet studentů
            StudentCount = teacher.Students.Count;

            // 5️⃣ Studenti s nízkým kreditem
            LowCreditCount = teacher.Students.Count(s => s.RemainingHours <= 1);

            // 6️⃣ Týdenní rozvrh
            var dayOfWeek = (int)today.DayOfWeek == 0 ? 7 : (int)today.DayOfWeek;
            var startOfWeek = today.AddDays(-(dayOfWeek - 1));
            var endOfWeek = startOfWeek.AddDays(7);

            var weekLessons = await _context.Lessons
    .Include(l => l.Student)
    .Include(l => l.Subject)
    .Include(l => l.SubjectTopic)
    .Where(l =>
        l.TeacherId == teacher.Id &&
        l.Date >= startOfWeek &&
        l.Date < endOfWeek
    )
    .OrderBy(l => l.Date)
    .ThenBy(l => l.Start)
    .ToListAsync();


            WeeklySchedule = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var date = startOfWeek.AddDays(offset);

                    var lessonsForDay = weekLessons
                        .Where(l => l.Date.Date == date.Date)
                        .OrderBy(l => l.Start)
                        .ToList();

                    return new DaySchedule
                    {
                        Day = GetCzechDayName(date.DayOfWeek),
                        Date = date,
                        Lessons = lessonsForDay
                    };
                })
                .ToList();

            // 7️⃣ Upozornění
            Notifications = _context.Notes
                .Include(n => n.Student)
                .Where(n => n.TeacherId == teacher.Id)
                .OrderByDescending(n => n.Created)
                .Take(5)
                .Select(n => $"{n.Created:dd.MM.yyyy HH:mm} – {n.Student.FirstName} {n.Student.LastName}: {n.Text}")
                .ToList();
        }

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

        public class DaySchedule
        {
            public string Day { get; set; }
            public DateTime Date { get; set; }
            public List<Lesson> Lessons { get; set; } = new();
        }
    }
}
