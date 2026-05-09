using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Students_only
{
    public class PaymentsModel : PageModel
    {
        private readonly AppDbContext _db;

        public PaymentsModel(AppDbContext db)
        {
            _db = db;
        }

        public string StudentName { get; set; } = "";

        public decimal TotalPurchasedHours { get; set; }
        public decimal TotalPaidAmount { get; set; }

        public List<PaymentRow> Payments { get; set; } = new();

        public class PaymentRow
        {
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }
            public decimal HoursPurchased { get; set; }
            public string Method { get; set; } = "";
        }

        public async Task OnGetAsync()
        {
            int studentId = 1;

            var student = await _db.Students.FindAsync(studentId);
            StudentName = student.FullName;

            var payments = await _db.Payments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            TotalPurchasedHours = payments.Sum(p => p.HoursPurchased);
            TotalPaidAmount = payments.Sum(p => p.Amount);

            Payments = payments.Select(p => new PaymentRow
            {
                Date = p.Date,
                Amount = p.Amount,
                HoursPurchased = p.HoursPurchased,
                Method = p.Method ?? "—"
            }).ToList();

        }
    }
}
