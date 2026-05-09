using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Students_only
{
    public class PaymentsModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public PaymentsModel(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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
            // 1) Přihlášený Identity uživatel
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return;

            // 2) Najdeme studenta podle emailu (bez migrací)
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.Email == user.Email);

            if (student == null)
                return;

            int studentId = student.Id;
            StudentName = student.FullName;

            // 3) Zbytek kódu necháváš tak, jak je
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
