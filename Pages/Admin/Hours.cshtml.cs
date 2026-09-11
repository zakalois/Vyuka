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

        // Souhrn za vybrané období
        public double IntervalTaughtHours { get; set; }
        public int IntervalLessonsCount { get; set; }

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
        public double TotalTaughtStudentHours { get; set; }

        public UnifiedLesson? NextLesson { get; set; }

        public async Task OnGetAsync(
            int? studentId,
            DateTime? from,
            DateTime? to,
            int monthOffset = 0)
        {
            SelectedStudentId = studentId;

            var yearStart = new DateTime(DateTime.Today.Year, 1, 1);

            // MĚSÍČNÍ FILTR – pokud není ruční datum
            if (!from.HasValue && !to.HasValue)
            {
                var baseDate = DateTime.Today.AddMonths(monthOffset);

                From = new DateTime(baseDate.Year, baseDate.Month, 1);
                To = From.Value.AddMonths(1).AddDays(-1);
            }
            else
            {
                From = (from.HasValue && from.Value != DateTime.MinValue) ? from : null;
                To = (to.HasValue && to.Value != DateTime.MinValue) ? to : null;
            }

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

            var taughtYearDecimal = taughtLessonsYear.Sum(l =>
                l.End > l.Start
                    ? (decimal)Math.Round((l.End - l.Start).TotalHours, 1)
                    : l.Hours
            );

            TotalTaughtHours = (double)taughtYearDecimal;
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

            // plánované hodiny studenta
            var plans = await _context.LessonPlans
                .Where(lp => lp.StudentId == studentId)
                .ToListAsync();

            // odučené hodiny studenta
            var lessons = await _context.Lessons
                .Where(l => l.StudentId == studentId)
                .ToListAsync();

            // sjednocená tabulka
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

            // filtr sjednocené tabulky
            unified = unified
                .Where(u =>
                    (!From.HasValue || u.Date >= From.Value) &&
                    (!To.HasValue || u.Date <= To.Value)
                )
                .ToList();

            // Souhrn za období – pouze odučené
            var taughtIntervalUnified = unified
                .Where(u => u.IsTaught)
                .ToList();

            IntervalTaughtHours = taughtIntervalUnified.Sum(u =>
                u.End > u.Start
                    ? (u.End - u.Start).TotalHours
                    : 0
            );

            IntervalLessonsCount = taughtIntervalUnified.Count;

            // plánované v intervalu
            var plannedInterval = plans
                .Where(lp =>
                    (!From.HasValue || lp.Date >= From.Value) &&
                    (!To.HasValue || lp.Date <= To.Value)
                )
                .ToList();

            PlannedHours = plannedInterval.Sum(lp => (lp.End - lp.Start).TotalHours);

            // odučené v intervalu
            var taughtInterval = lessons
                .Where(l =>
                    l.IsTaught &&
                    (!From.HasValue || l.Date >= From.Value) &&
                    (!To.HasValue || l.Date <= To.Value)
                )
                .ToList();

            TaughtHours = taughtInterval.Sum(l =>
                l.End > l.Start
                    ? (l.End - l.Start).TotalHours
                    : (double)l.Hours
            );

            // CELKEM ODUČENO – všechny odučené hodiny studenta
            var taughtAllTime = lessons
                .Where(l => l.IsTaught)
                .Sum(l =>
                    l.End > l.Start
                        ? (l.End - l.Start).TotalHours
                        : (double)l.Hours
                );

            TotalTaughtStudentHours = taughtAllTime;

            // ZBÝVÁ CELKEM
            RemainingHours = PaidHours - taughtAllTime;

            // řazení
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
