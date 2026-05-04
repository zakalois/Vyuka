using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin
{
    public class HoursModel : PageModel
    {
        private readonly AppDbContext _context;

        public HoursModel(AppDbContext context)
        {
            _context = context;
        }

        // Filtr
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // Data
        public IList<Student> Students { get; set; } = new List<Student>();
        public IList<UnifiedLesson> UnifiedLessons { get; set; } = new List<UnifiedLesson>();

        public Student? SelectedStudent { get; set; }
        public int? SelectedStudentId { get; set; }

        // Horní přehled
        public double TotalPrepaidHours { get; set; }
        public double TotalTaughtHours { get; set; }
        public double TotalRemainingHours { get; set; }

        // Souhrn studenta
        public double PaidHours { get; set; }
        public double PlannedHours { get; set; }
        public double TaughtHours { get; set; }
        public double RemainingHours { get; set; }

        public UnifiedLesson? NextLesson { get; set; }

        public async Task OnGetAsync(int? studentId, DateTime? from, DateTime? to)
        {
            SelectedStudentId = studentId;

            var yearStart = new DateTime(DateTime.Today.Year, 1, 1);

            From = from ?? yearStart;
            To = to;

            // studenti
            Students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // horní přehled – předplacené
            var prepaidDecimal = await _context.Payments
                .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0m;

            TotalPrepaidHours = (double)prepaidDecimal;

            // horní přehled – odučené (za celý rok)
            var taughtLessonsYear = await _context.Lessons
                .Where(l => l.Date >= yearStart && l.IsTaught)
                .ToListAsync();

            TotalTaughtHours = taughtLessonsYear
                .Sum(l => l.End > l.Start
                    ? (l.End - l.Start).TotalHours
                    : (double)l.Hours);

            TotalRemainingHours = TotalPrepaidHours - TotalTaughtHours;

            // pokud není vybrán student → konec
            if (studentId == null)
                return;

            SelectedStudent = Students.FirstOrDefault(s => s.Id == studentId);
            if (SelectedStudent == null)
                return;

            // zaplacené hodiny studenta
            var paidDecimal = await _context.Payments
                .Where(p => p.StudentId == studentId)
                .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0m;

            PaidHours = (double)paidDecimal;

            // plánované hodiny studenta (LessonPlans)
            var plans = await _context.LessonPlans
                .Where(lp => lp.StudentId == studentId)
                .ToListAsync();

            // odučené hodiny studenta (Lessons)
            var lessons = await _context.Lessons
                .Where(l => l.StudentId == studentId)
                .ToListAsync();

            // sjednocená tabulka pro zobrazení
            var unified = new List<UnifiedLesson>();

            unified.AddRange(plans.Select(lp => new UnifiedLesson
            {
                Date = lp.Date,
                Start = lp.Start,
                End = lp.End,
                IsTaught = false,
                MeetLink = lp.MeetLink
            }));

            unified.AddRange(lessons.Select(l => new UnifiedLesson
            {
                Date = l.Date,
                Start = l.Start,
                End = l.End,
                IsTaught = true,
                MeetLink = l.MeetLink
            }));

            // filtr pro tabulku
            unified = unified
                .Where(u => u.Date >= From.Value && (!To.HasValue || u.Date <= To.Value))
                .ToList();

            // plánované v intervalu – z LessonPlans
            var plannedInterval = plans
                .Where(lp => lp.Date >= From.Value && (!To.HasValue || lp.Date <= To.Value))
                .ToList();

            PlannedHours = plannedInterval
                .Sum(lp => (lp.End - lp.Start).TotalHours);

            // odučené v intervalu – z Lessons (s fallbackem na Hours)
            var taughtInterval = lessons
                .Where(l => l.IsTaught &&
                            l.Date >= From.Value &&
                            (!To.HasValue || l.Date <= To.Value))
                .ToList();

            TaughtHours = taughtInterval
                .Sum(l => l.End > l.Start
                    ? (l.End - l.Start).TotalHours
                    : (double)l.Hours);

            // zbývá = zaplacené – odučené CELKEM (za rok)
            var taughtAllYear = lessons
                .Where(l => l.IsTaught && l.Date >= yearStart)
                .Sum(l => l.End > l.Start
                    ? (l.End - l.Start).TotalHours
                    : (double)l.Hours);

            RemainingHours = PaidHours - taughtAllYear;

            // řazení tabulky – nejmladší nahoře, plánované nad odučenými
            UnifiedLessons = unified
                .OrderByDescending(u => u.Date)
                .ThenBy(u => u.IsTaught)
                .ToList();

            // další plánovaná hodina
            var today = DateTime.Today;
            var now = DateTime.Now.TimeOfDay;

            NextLesson = plans
                .Where(lp =>
                    lp.Date >= today.AddDays(-1) &&
                    (
                        lp.Date > today ||
                        (lp.Date == today && lp.Start > now) ||
                        lp.Date == today.AddDays(-1)
                    )
                )
                .OrderBy(lp => lp.Date)
                .ThenBy(lp => lp.Start)
                .Select(lp => new UnifiedLesson
                {
                    Date = lp.Date,
                    Start = lp.Start,
                    End = lp.End,
                    IsTaught = false,
                    MeetLink = lp.MeetLink
                })
                .FirstOrDefault();
        }
    }

    public class UnifiedLesson
    {
        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public bool IsTaught { get; set; }
        public string? MeetLink { get; set; }
    }
}
