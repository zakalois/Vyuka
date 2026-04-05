using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        // Filtr období
        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        // Celkové součty
        public decimal TotalPurchasedHours { get; set; }
        public decimal TotalTaughtHours { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalBalanceHours => TotalPurchasedHours - TotalTaughtHours;

        // Model pro řádky studentů
        public class StudentSummary
        {
            public string Name { get; set; } = "";
            public decimal PurchasedHours { get; set; }
            public decimal TaughtHours { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal BalanceHours => PurchasedHours - TaughtHours;
        }

        public List<StudentSummary> Summaries { get; set; } = new();

        public async Task OnGetAsync()
        {
            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            foreach (var s in students)
            {
                // Filtr plateb podle období
                var paymentsQuery = _context.Payments
                    .Where(p => p.StudentId == s.Id);

                if (FromDate.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Date >= FromDate.Value);

                if (ToDate.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Date <= ToDate.Value);

                var purchasedHours = await paymentsQuery.SumAsync(p => p.HoursPurchased);
                var paidAmount = await paymentsQuery.SumAsync(p => p.Amount);

                var taughtHours = await _context.Lessons
                    .Where(l => l.StudentId == s.Id && l.IsTaught)
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
        }
    }
}