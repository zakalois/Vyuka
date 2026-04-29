using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Payments
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        // Filtry
        [BindProperty(SupportsGet = true)]
        public int SelectedStudentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        // Dropdown studentů
        public SelectList StudentList { get; set; } = default!;

        // Globální přehled
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int InactiveStudents { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPrepaidHours { get; set; }
        public decimal TotalTaughtHours { get; set; }

        // Přehled studenta
        public decimal SelectedStudentPrepaidHours { get; set; }
        public decimal SelectedStudentTaughtHours { get; set; }
        public decimal SelectedStudentBalance => SelectedStudentPrepaidHours - SelectedStudentTaughtHours;

        // Platební statistiky studenta
        public decimal SelectedStudentTotalPaid { get; set; }
        public int SelectedStudentPaymentCount { get; set; }
        public DateTime? SelectedStudentLastPaymentDate { get; set; }
        public decimal SelectedStudentLastPaymentAmount { get; set; }
        public decimal SelectedStudentLastPaymentHours { get; set; }

        // Tabulka plateb
        public List<Payment> Payments { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Dropdown studentů
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

            // Globální přehled
            TotalStudents = await _context.Students.CountAsync();
            ActiveStudents = await _context.Students.CountAsync(s => s.IsActive);
            InactiveStudents = TotalStudents - ActiveStudents;

            TotalPaid = await _context.Payments
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            TotalPrepaidHours = await _context.Payments
                .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0;

            TotalTaughtHours = await _context.Lessons
                .SumAsync(l => (decimal?)l.Hours) ?? 0;

            // Základní dotaz na platby
            var query = _context.Payments
                .Include(p => p.Student)
                .AsQueryable();

            if (SelectedStudentId > 0)
                query = query.Where(p => p.StudentId == SelectedStudentId);

            if (FromDate.HasValue)
                query = query.Where(p => p.Date >= FromDate.Value);

            if (ToDate.HasValue)
                query = query.Where(p => p.Date <= ToDate.Value);

            Payments = await query
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            // Přehled a statistiky vybraného studenta
            if (SelectedStudentId > 0)
            {
                SelectedStudentPrepaidHours = await _context.Payments
                    .Where(p => p.StudentId == SelectedStudentId)
                    .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0;

                SelectedStudentTaughtHours = await _context.Lessons
                    .Where(l => l.StudentId == SelectedStudentId)
                    .SumAsync(l => (decimal?)l.Hours) ?? 0;

                SelectedStudentTotalPaid = await _context.Payments
                    .Where(p => p.StudentId == SelectedStudentId)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                SelectedStudentPaymentCount = await _context.Payments
                    .CountAsync(p => p.StudentId == SelectedStudentId);

                var lastPayment = await _context.Payments
                    .Where(p => p.StudentId == SelectedStudentId)
                    .OrderByDescending(p => p.Date)
                    .FirstOrDefaultAsync();

                if (lastPayment != null)
                {
                    SelectedStudentLastPaymentDate = lastPayment.Date;
                    SelectedStudentLastPaymentAmount = lastPayment.Amount;
                    SelectedStudentLastPaymentHours = lastPayment.HoursPurchased;
                }
            }
        }
    }
}
