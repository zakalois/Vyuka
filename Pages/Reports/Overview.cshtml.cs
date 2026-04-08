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

        // 🔵 Filtr období
        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        // 🔵 Filtr studenta
        [BindProperty(SupportsGet = true)]
        public int SelectedStudentId { get; set; }

        public SelectList StudentList { get; set; } = default!;

        // 🔵 Celkové součty
        public decimal TotalPurchasedHours { get; set; }
        public decimal TotalTaughtHours { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalBalanceHours => TotalPurchasedHours - TotalTaughtHours;

        // 🔵 Model pro řádky studentů (globální tabulka)
        public class StudentSummary
        {
            public string Name { get; set; } = "";
            public decimal PurchasedHours { get; set; }
            public decimal TaughtHours { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal BalanceHours => PurchasedHours - TaughtHours;
        }

        public List<StudentSummary> Summaries { get; set; } = new();

        // 🔵 Model pro výpis výuky studenta
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

        // 🔵 GET – načtení dat
        public async Task OnGetAsync()
        {
            // 🔹 Dropdown studentů
            StudentList = new SelectList(
                await _context.Students
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => new { s.Id, FullName = s.LastName + " " + s.FirstName })
                    .ToListAsync(),
                "Id",
                "FullName"
            );

            // 🔹 Globální přehled výuky a plateb
            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            foreach (var s in students)
            {
                var paymentsQuery = _context.Payments
                    .Where(p => p.StudentId == s.Id);

                if (FromDate.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Date.Date >= FromDate.Value.Date);

                if (ToDate.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Date.Date <= ToDate.Value.Date);

                var purchasedHours = await paymentsQuery.SumAsync(p => p.HoursPurchased);
                var paidAmount = await paymentsQuery.SumAsync(p => p.Amount);

                var taughtHours = await _context.Lessons
                    .Where(l => l.StudentId == s.Id && l.IsTaught)
                    .Where(l => !FromDate.HasValue || l.Date.Date >= FromDate.Value.Date)
                    .Where(l => !ToDate.HasValue || l.Date.Date <= ToDate.Value.Date)
                    .SumAsync(l => l.Hours);

                Summaries.Add(new StudentSummary
                {
                    Name = $"{s.LastName} {s.FirstName}",
                    PurchasedHours = purchasedHours,
                    TaughtHours = taughtHours,
                    PaidAmount = paidAmount
                });

                TotalPurchasedHours += purchasedHours;
                TotalTaughtHours += taughtHours;
                TotalPaidAmount += paidAmount;
            }

            // 🔹 Výpis výuky pro vybraného studenta
            if (SelectedStudentId > 0)
            {
                // 1️⃣ Načteme odučené hodiny
                var taught = await _context.Lessons
                    .Include(l => l.Subject)
                    .Where(l => l.StudentId == SelectedStudentId)
                    .Where(l => !FromDate.HasValue || l.Date.Date >= FromDate.Value.Date)
                    .Where(l => !ToDate.HasValue || l.Date.Date <= ToDate.Value.Date)
                    .ToListAsync();

                // 2️⃣ Načteme plánované hodiny
                var plans = await _context.LessonPlans
                    .Include(p => p.Subject)
                    .Include(p => p.SubjectTopic)
                    .Where(p => p.StudentId == SelectedStudentId)
                    .ToListAsync();

                // 3️⃣ Odučené hodiny + dohledání tématu
                var taughtEvents = taught.Select(l =>
                {
                    var matchingPlan = plans.FirstOrDefault(p =>
                        p.StudentId == l.StudentId &&
                        p.SubjectId == l.SubjectId &&
                        p.Date.Date == l.Date.Date &&
                        (int)(p.End - p.Start).TotalHours == l.Hours
                    );

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

                // 4️⃣ Plánované hodiny
                var plannedEvents = plans
                    .Where(s => !FromDate.HasValue || s.Date.Date >= FromDate.Value.Date)
                    .Where(s => !ToDate.HasValue || s.Date.Date <= ToDate.Value.Date)
                    .Select(s => new StudentLessonEvent
                    {
                        Date = s.Date,
                        Type = "Plánovaná hodina",
                        Hours = (decimal)(s.End - s.Start).TotalHours,
                        Subject = s.Subject?.Name ?? "",
                        Topic = s.SubjectTopic?.Name ?? "",
                        ColorClass = "lesson-planned"
                    })
                    .ToList();

                // 5️⃣ Spojit a seřadit
                StudentLessons = taughtEvents
                    .Concat(plannedEvents)
                    .OrderByDescending(e => e.Date)
                    .ToList();
            }
        }
    }
}