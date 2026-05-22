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

        [BindProperty(SupportsGet = true)]
        public int SelectedStudentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public SelectList StudentList { get; set; } = default!;

        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int InactiveStudents { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPrepaidHours { get; set; }
        public decimal TotalTaughtHours { get; set; }

        public decimal SelectedStudentPrepaidHours { get; set; }
        public decimal SelectedStudentTaughtHours { get; set; }
        public decimal SelectedStudentBalance => SelectedStudentPrepaidHours - SelectedStudentTaughtHours;

        public decimal SelectedStudentTotalPaid { get; set; }
        public int SelectedStudentPaymentCount { get; set; }
        public DateTime? SelectedStudentLastPaymentDate { get; set; }
        public decimal SelectedStudentLastPaymentAmount { get; set; }
        public decimal SelectedStudentLastPaymentHours { get; set; }

        public List<Payment> Payments { get; set; } = new();

        public async Task OnGetAsync()
        {
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

            TotalStudents = await _context.Students.CountAsync();
            ActiveStudents = await _context.Students.CountAsync(s => s.IsActive);
            InactiveStudents = TotalStudents - ActiveStudents;

            TotalPaid = await _context.Payments
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // ⭐ OPRAVA – zaokrouhlení na 1 desetinné místo
            TotalPrepaidHours = Math.Round(
                await _context.Payments.SumAsync(p => (decimal?)p.HoursPurchased) ?? 0, 1);

            var taughtLessons = await _context.Lessons
    .Where(l => l.IsTaught)
    .ToListAsync();

            TotalTaughtHours = Math.Round(
                taughtLessons.Sum(l =>
                    l.End > l.Start
                        ? (decimal)Math.Round((l.End - l.Start).TotalHours, 1)
                        : l.Hours
                ), 1);


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

            if (SelectedStudentId > 0)
            {
                SelectedStudentPrepaidHours = Math.Round(
                    await _context.Payments
                        .Where(p => p.StudentId == SelectedStudentId)
                        .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0, 1);

                var taughtStudentLessons = await _context.Lessons
    .Where(l => l.StudentId == SelectedStudentId && l.IsTaught)
    .ToListAsync();

                SelectedStudentTaughtHours = Math.Round(
                    taughtStudentLessons.Sum(l =>
                        l.End > l.Start
                            ? (decimal)Math.Round((l.End - l.Start).TotalHours, 1)
                            : l.Hours
                    ), 1);


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
