using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Reports
{
    public class OverviewModel : PageModel
    {
        private readonly AppDbContext _context;

        public OverviewModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        // STRING – kvůli hodnotám ".1", ".2", ...
        [BindProperty(SupportsGet = true)]
        public string SelectedStudentId { get; set; } = "";

        // Dvě kolekce – skupiny + studenti
        public List<SelectListItem> GroupItems { get; set; } = new();
        public List<SelectListItem> StudentItems { get; set; } = new();

        // Souhrny
        public decimal TotalPurchasedHours { get; set; }
        public decimal TotalTaughtHours { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalPlannedHours { get; set; }
        public decimal TotalRemainingHours { get; set; }

        public class StudentSummary
        {
            public string Name { get; set; } = "";
            public decimal PurchasedHours { get; set; }
            public decimal TaughtHours { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal PlannedHours { get; set; }
            public bool IsActive { get; set; }
            public decimal RemainingHoursToTeach => PurchasedHours - TaughtHours;
        }

        public List<StudentSummary> Summaries { get; set; } = new();

        public class StudentLessonEvent
        {
            public DateTime Date { get; set; }
            public string Type { get; set; } = "";
            public decimal Hours { get; set; }
            public string Subject { get; set; } = "";
            public string Topic { get; set; } = "";
            public string ColorClass { get; set; } = "";
        }

        public List<StudentLessonEvent> StudentLessons { get; set; } = new();

        public async Task OnGetAsync()
        {
            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // Remaining hours
            var remainingHoursMap = new Dictionary<int, decimal>();

            foreach (var s in students)
            {
                var purchased = await _context.Payments
                    .Where(p => p.StudentId == s.Id)
                    .SumAsync(p => p.HoursPurchased);

                var taught = await _context.Lessons
                    .Where(l => l.StudentId == s.Id && l.IsTaught)
                    .SumAsync(l => l.Hours);

                remainingHoursMap[s.Id] = purchased - taught;
            }

            // SKUPINY
            GroupItems = new List<SelectListItem>
            {
                new SelectListItem { Value = ".1", Text = "1) Aktivní studenti" },
                new SelectListItem { Value = ".2", Text = "2) Neaktivní studenti" },
                new SelectListItem { Value = ".3", Text = "3) Málo hodin (< 5)" },
                new SelectListItem { Value = ".4", Text = "4) Hodně hodin (>= 5)" },
                new SelectListItem { Value = ".5", Text = "5) Všichni studenti" }
            };

            // STUDENTI – abecedně
            StudentItems = students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.LastName} {s.FirstName}"
                })
                .ToList();

            // FILTROVÁNÍ
            IEnumerable<Student> filteredStudents = students;

            if (SelectedStudentId.StartsWith(".1"))
                filteredStudents = students.Where(s => s.IsActive);

            else if (SelectedStudentId.StartsWith(".2"))
                filteredStudents = students.Where(s => !s.IsActive);

            else if (SelectedStudentId.StartsWith(".3"))
                filteredStudents = students.Where(s => remainingHoursMap[s.Id] < 5);

            else if (SelectedStudentId.StartsWith(".4"))
                filteredStudents = students.Where(s => remainingHoursMap[s.Id] >= 5);

            else if (SelectedStudentId.StartsWith(".5"))
                filteredStudents = students;

            else if (int.TryParse(SelectedStudentId, out int studentId))
                filteredStudents = students.Where(s => s.Id == studentId);

            // SOUHRNNÁ TABULKA
            foreach (var s in filteredStudents)
            {
                var paymentsQuery = _context.Payments.Where(p => p.StudentId == s.Id);

                if (FromDate.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Date.Date >= FromDate.Value.Date);

                if (ToDate.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Date.Date <= ToDate.Value.Date);

                var purchasedHours = await paymentsQuery.SumAsync(p => p.HoursPurchased);
                var paidAmount = await paymentsQuery.SumAsync(p => p.Amount);

                var taughtLessons = await _context.Lessons
                    .Where(l => l.StudentId == s.Id && l.IsTaught)
                    .Where(l => !FromDate.HasValue || l.Date.Date >= FromDate.Value.Date)
                    .Where(l => !ToDate.HasValue || l.Date.Date <= ToDate.Value.Date)
                    .ToListAsync();

                var taughtHours = taughtLessons.Sum(l => l.Hours);

                var plannedLessons = await _context.LessonPlans
                    .Where(lp => lp.StudentId == s.Id)
                    .Where(lp => !FromDate.HasValue || lp.Date.Date >= FromDate.Value.Date)
                    .Where(lp => !ToDate.HasValue || lp.Date.Date <= ToDate.Value.Date)
                    .ToListAsync();

                var remainingPlanned = plannedLessons
                    .Where(pl => !taughtLessons.Any(t =>
                        t.Date.Date == pl.Date.Date &&
                        t.SubjectId == pl.SubjectId &&
                        Math.Abs((decimal)(pl.End - pl.Start).TotalHours - t.Hours) < 0.01m))
                    .ToList();

                var plannedHours = remainingPlanned.Sum(pl => (decimal)(pl.End - pl.Start).TotalHours);

                Summaries.Add(new StudentSummary
                {
                    Name = $"{s.LastName} {s.FirstName}",
                    PurchasedHours = purchasedHours,
                    TaughtHours = taughtHours,
                    PaidAmount = paidAmount,
                    PlannedHours = plannedHours,
                    IsActive = s.IsActive
                });

                TotalPurchasedHours += purchasedHours;
                TotalTaughtHours += taughtHours;
                TotalPaidAmount += paidAmount;
                TotalPlannedHours += plannedHours;
                TotalRemainingHours += (purchasedHours - taughtHours);
            }

            // DETAIL STUDENTA
            if (int.TryParse(SelectedStudentId, out int selectedId) && selectedId > 0)
            {
                var taught = await _context.Lessons
                    .Include(l => l.Subject)
                    .Where(l => l.StudentId == selectedId)
                    .ToListAsync();

                var plans = await _context.LessonPlans
                    .Include(p => p.Subject)
                    .Include(p => p.SubjectTopic)
                    .Where(p => p.StudentId == selectedId)
                    .ToListAsync();

                var taughtEvents = taught.Select(l =>
                {
                    var matchingPlan = plans.FirstOrDefault(p =>
                        p.StudentId == l.StudentId &&
                        p.SubjectId == l.SubjectId &&
                        p.Date.Date == l.Date.Date &&
                        Math.Abs((decimal)(p.End - p.Start).TotalHours - l.Hours) < 0.01m);

                    return new StudentLessonEvent
                    {
                        Date = l.Date,
                        Type = "Odučená hodina",
                        Hours = l.Hours,
                        Subject = l.Subject?.Name ?? "",
                        Topic = matchingPlan?.SubjectTopic?.Name ?? "",
                        ColorClass = "lesson-taught"
                    };
                }).ToList();

                var plannedEvents = plans.Select(s => new StudentLessonEvent
                {
                    Date = s.Date,
                    Type = "Plánovaná hodina",
                    Hours = (decimal)(s.End - s.Start).TotalHours,
                    Subject = s.Subject?.Name ?? "",
                    Topic = s.SubjectTopic?.Name ?? "",
                    ColorClass = "lesson-planned"
                }).ToList();

                StudentLessons = taughtEvents
                    .Concat(plannedEvents)
                    .OrderByDescending(e => e.Date)
                    .ToList();
            }
        }
    }
}
