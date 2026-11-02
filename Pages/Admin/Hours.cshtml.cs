using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Vyuka.Models;
using Vyuka.PDF;

namespace Vyuka.Pages.Admin
{
    public class HoursModel : PageModel
    {
        private readonly AppDbContext _context;

        public HoursModel(AppDbContext context)
        {
            _context = context;
        }

        // ⭐ Filtr – musí podporovat GET
        [BindProperty(SupportsGet = true)]
        public DateTime? From { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? To { get; set; }

        // ⭐ Student – také musí podporovat GET
        [BindProperty(SupportsGet = true)]
        public int? SelectedStudentId { get; set; }

        // Data
        public IList<Student> Students { get; set; } = new List<Student>();
        public IList<UnifiedLesson> UnifiedLessons { get; set; } = new List<UnifiedLesson>();

        public Student? SelectedStudent { get; set; }

        // ⭐ Souhrny za období
        public double IntervalTaughtHours { get; set; }
        public int IntervalLessonsCount { get; set; }
        public double PlannedHours { get; set; }
        public double TaughtHours { get; set; }

        // ⭐ Horní přehled
        public double TotalPrepaidHours { get; set; }
        public double TotalTaughtHours { get; set; }
        public double TotalRemainingHours { get; set; }

        // ⭐ Souhrny studenta
        public double PaidHours { get; set; }
        public double RemainingHours { get; set; }
        public double TotalTaughtStudentHours { get; set; }

        // ⭐ Další plánovaná hodina
        public UnifiedLesson? NextLesson { get; set; }


        // ⭐ VRÁCENÁ FUNKČNÍ SIGNATURA
        public async Task OnGetAsync(int? studentId, DateTime? from, DateTime? to)
        {
            SelectedStudentId = studentId;
            From = from;
            To = to;

            // studenti
            Students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            if (SelectedStudentId == null)
                return;

            SelectedStudent = Students.FirstOrDefault(s => s.Id == SelectedStudentId);
            if (SelectedStudent == null)
                return;

            // zaplacené hodiny studenta
            var paidDecimal = await _context.Payments
                .Where(p => p.StudentId == SelectedStudentId)
                .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0m;

            PaidHours = (double)paidDecimal;

            // načtení plánovaných hodin
            var plans = await _context.LessonPlans
                .Where(lp => lp.StudentId == SelectedStudentId)
                .ToListAsync();

            // načtení odučených hodin
            var lessons = await _context.Lessons
                .Where(l => l.StudentId == SelectedStudentId)
                .ToListAsync();

            // sjednocená tabulka – PLÁNOVANÉ + ODUČENÉ
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
                IsTaught = l.IsTaught,
                MeetLink = l.MeetLink
            }));

            // ⭐ FILTR
            unified = unified
                .Where(u =>
                    (!From.HasValue || u.Date >= From.Value) &&
                    (!To.HasValue || u.Date <= To.Value)
                )
                .ToList();

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

            IntervalLessonsCount = taughtInterval.Count;
            IntervalTaughtHours = TaughtHours;

            // CELKEM ODUČENO
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

            // ⭐ řazení – nejnovější nahoře
            UnifiedLessons = unified
                .OrderByDescending(u => u.Date)
                .ThenByDescending(u => u.Start)
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


        // ⭐ PDF EXPORT – plánované + odučené, bez duplicit
        public IActionResult OnGetExportHoursPdf(int studentId, DateTime? from, DateTime? to)
        {
            var student = _context.Students.Find(studentId);

            var paidDecimal = _context.Payments
                .Where(p => p.StudentId == studentId)
                .Sum(p => (decimal?)p.HoursPurchased) ?? 0m;

            var totalPaidHours = (int)paidDecimal;

            var plans = _context.LessonPlans
                .Where(lp => lp.StudentId == studentId)
                .ToList();

            var lessons = _context.Lessons
                .Where(l => l.StudentId == studentId)
                .ToList();

            // ⭐ Odstranění duplicit
            var filteredPlans = plans
                .Where(lp => !lessons.Any(l =>
                    l.Date == lp.Date &&
                    l.Start == lp.Start &&
                    l.End == lp.End))
                .ToList();

            // ⭐ Výpočet plánovaných neodučených hodin
            var plannedNotTaught = filteredPlans.Sum(lp => (lp.End - lp.Start).TotalHours);

            // ⭐ Sjednocení
            var unified = new List<LessonDto>();

            unified.AddRange(filteredPlans.Select(lp => new LessonDto
            {
                Date = lp.Date,
                Start = lp.Start,
                End = lp.End,
                Hours = (int)Math.Round((lp.End - lp.Start).TotalHours),
                Type = "Plánováno"
            }));

            unified.AddRange(lessons.Select(l => new LessonDto
            {
                Date = l.Date,
                Start = l.Start,
                End = l.End,
                Hours = (int)Math.Round(
                    l.End > l.Start
                        ? (l.End - l.Start).TotalHours
                        : (double)l.Hours),
                Type = l.IsTaught ? "Odučeno" : "Plánováno"
            }));

            // ⭐ Filtrování
            if (from.HasValue)
                unified = unified.Where(u => u.Date >= from.Value).ToList();

            if (to.HasValue)
                unified = unified.Where(u => u.Date <= to.Value).ToList();

            // ⭐ Řazení – nejnovější nahoře
            unified = unified
                .OrderByDescending(u => u.Date)
                .ThenByDescending(u => u.Start)
                .ToList();

            // ⭐ Číslování
            for (int i = 0; i < unified.Count; i++)
                unified[i].Order = i + 1;

            var totalTaughtHours = unified
                .Where(u => u.Type == "Odučeno")
                .Sum(u => u.Hours);

            var remaining = totalPaidHours - totalTaughtHours;

            var doc = new LessonsReportDocument
            {
                StudentName = student.FullName,
                TotalHours = totalPaidHours,
                TaughtHours = totalTaughtHours,
                RemainingHours = remaining,
                PlannedNotTaughtHours = (int)Math.Round(plannedNotTaught),
                Lessons = unified
            };

            var pdfBytes = doc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"hodiny_{student.FullName}.pdf");
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
