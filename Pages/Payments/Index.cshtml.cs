using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Payments
{
    public class PaymentsIndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public PaymentsIndexModel(AppDbContext context)
        {
            _context = context;
        }

        // 🔵 Globální statistiky
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int InactiveStudents { get; set; }

        public decimal TotalPaidAmount { get; set; }
        public decimal TotalPrepaidHours { get; set; }
        public decimal TotalTaughtHours { get; set; }

        public decimal TotalBalance => TotalPrepaidHours - TotalTaughtHours;

        // 🔵 Globální tabulka studentů
        public class GlobalPaymentRow
        {
            public int StudentId { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public decimal PrepaidHours { get; set; }
            public decimal TaughtHours { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal Balance => PrepaidHours - TaughtHours;
        }

        public List<GlobalPaymentRow> GlobalTable { get; set; } = new();

        // 🔵 Detail vybraného studenta
        public decimal SelectedStudentPrepaidHours { get; set; }
        public decimal SelectedStudentTaughtHours { get; set; }
        public decimal SelectedStudentBalance => SelectedStudentPrepaidHours - SelectedStudentTaughtHours;

        // 🔵 Dropdown + platby
        public SelectList StudentList { get; set; } = default!;
        public List<Payment> Payments { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int SelectedStudentId { get; set; }

        // 🔵 Datumový filtr
        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        // 🔵 GET – načtení dat
        public async Task OnGetAsync()
        {
            // 🔹 Globální statistiky
            TotalStudents = await _context.Students.CountAsync();
            ActiveStudents = await _context.Students.CountAsync(s => s.IsActive);
            InactiveStudents = TotalStudents - ActiveStudents;

            TotalPaidAmount = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
            TotalPrepaidHours = await _context.Payments.SumAsync(p => (decimal?)p.HoursPurchased) ?? 0;
            TotalTaughtHours = await _context.Lessons.SumAsync(l => (decimal?)l.Hours) ?? 0;

            // 🔹 Globální tabulka studentů
            var allStudents = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            GlobalTable = new List<GlobalPaymentRow>();

            foreach (var s in allStudents)
            {
                var prepaid = await _context.Payments
                    .Where(p => p.StudentId == s.Id)
                    .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0;

                var taught = await _context.Lessons
                    .Where(l => l.StudentId == s.Id)
                    .SumAsync(l => (decimal?)l.Hours) ?? 0;

                var paid = await _context.Payments
                    .Where(p => p.StudentId == s.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                GlobalTable.Add(new GlobalPaymentRow
                {
                    StudentId = s.Id,
                    Name = $"{s.LastName} {s.FirstName}",
                    IsActive = s.IsActive,
                    PrepaidHours = prepaid,
                    TaughtHours = taught,
                    TotalPaid = paid
                });
            }

            // 🔹 Dropdown studentů
            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            StudentList = new SelectList(
                students.Select(s => new
                {
                    Id = s.Id,
                    FullName = $"{s.LastName} {s.FirstName}"
                }),
                "Id",
                "FullName"
            );

            // 🔹 Platby vybraného studenta + datumový filtr
            if (SelectedStudentId > 0)
            {
                var paymentsQuery = _context.Payments
                    .Where(p => p.StudentId == SelectedStudentId);

                if (DateFrom.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Date >= DateFrom.Value);

                if (DateTo.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Date <= DateTo.Value);

                Payments = await paymentsQuery
                    .OrderByDescending(p => p.Date)
                    .ToListAsync();

                SelectedStudentPrepaidHours = Payments.Sum(p => p.HoursPurchased);

                SelectedStudentTaughtHours = await _context.Lessons
                    .Where(l => l.StudentId == SelectedStudentId)
                    .SumAsync(l => l.Hours);
            }
        }
    }
}