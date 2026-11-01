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

        // Filtr
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // Data
        public IList<Student> Students { get; set; } = new List<Student>();
        public IList<UnifiedLesson> UnifiedLessons { get; set; } = new List<UnifiedLesson>();

        public Student? SelectedStudent { get; set; }
        public int? SelectedStudentId { get; set; }

        // ⭐ Souhrny za období – stránka je potřebuje
        public double IntervalTaughtHours { get; set; }
        public int IntervalLessonsCount { get; set; }
        public double PlannedHours { get; set; }
        public double TaughtHours { get; set; }

        // ⭐ Horní přehled – stránka ho používá
        public double TotalPrepaidHours { get; set; }
        public double TotalTaughtHours { get; set; }
        public double TotalRemainingHours { get; set; }

        // ⭐ Souhrny studenta – celkové
        public double PaidHours { get; set; }
        public double RemainingHours { get; set; }
        public double TotalTaughtStudentHours { get; set; }

        // ⭐ Další plánovaná hodina – stránka ji také používá
        public UnifiedLesson? NextLesson { get; set; }


        public async Task OnGetAsync(int? studentId, DateTime? from, DateTime? to, int monthOffset = 0)
        {
            SelectedStudentId = studentId;

            // studenti
            Students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

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

            // načtení plánovaných hodin
            var plans = await _context.LessonPlans
                .Where(lp => lp.StudentId == studentId)
                .ToListAsync();

            // načtení odučených hodin
            var lessons = await _context.Lessons
                .Where(l => l.StudentId == studentId)
                .ToListAsync();

            // sjednocená tabulka – PLÁNOVANÉ + ODUČENÉ
            var unified = new List<UnifiedLesson>();

            // plánované (žluté)
            unified.AddRange(plans.Select(lp => new UnifiedLesson
            {
                Date = lp.Date,
                Start = lp.Start,
                End = lp.End,
                IsTaught = false,
                MeetLink = lp.MeetLink
            }));

            // odučené (zelené)
            unified.AddRange(lessons.Select(l => new UnifiedLesson
            {
                Date = l.Date,
                Start = l.Start,
                End = l.End,
                IsTaught = l.IsTaught,
                MeetLink = l.MeetLink
            }));

            // filtr sjednocené tabulky
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

        // ⭐ PDF EXPORT – kompletní historie + číslování + správné počty
        public IActionResult OnGetExportHoursPdf(int studentId, DateTime? from, DateTime? to)
        {
            var student = _context.Students.Find(studentId);

            // Zaplacené hodiny studenta
            var paidDecimal = _context.Payments
                .Where(p => p.StudentId == studentId)
                .Sum(p => (decimal?)p.HoursPurchased) ?? 0m;

            var totalPaidHours = (int)paidDecimal;

            // Všechny lekce studenta
            var allLessons = _context.Lessons
                .Where(l => l.StudentId == studentId)
                .OrderBy(l => l.Date)
                .ThenBy(l => l.Start)
                .ToList();

            // Celkem odučeno
            var totalTaughtHours = (int)Math.Round(allLessons
                .Where(l => l.IsTaught)
                .Sum(l =>
                    l.End > l.Start
                        ? (l.End - l.Start).TotalHours
                        : (double)l.Hours
                ));

            // Zbývá
            var remaining = totalPaidHours - totalTaughtHours;

            // Lekce pro PDF – číslování + celé hodiny
            var lessons = allLessons
                .Select((l, index) => new LessonDto
                {
                    Order = index + 1,
                    Date = l.Date,
                    Start = l.Start,
                    End = l.End,
                    Hours = (int)Math.Round(
                        l.End > l.Start
                            ? (l.End - l.Start).TotalHours
                            : (double)l.Hours),
                    Type = l.IsTaught ? "Odučeno" : "Plánováno"
                })
                .ToList();

            var doc = new LessonsReportDocument
            {
                StudentName = student.FullName,
                TotalHours = totalPaidHours,
                TaughtHours = totalTaughtHours,
                RemainingHours = remaining,
                Lessons = lessons
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
