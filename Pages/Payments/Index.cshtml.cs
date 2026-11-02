using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
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

        // ⭐ Hodnoty pro PDF
        public int PaymentCount { get; set; }
        public int PrepaidHours { get; set; }
        public int TaughtHours { get; set; }
        public decimal LastPricePerHour { get; set; }

        // ⭐ Hodnoty, které očekává .cshtml (původní model)
        public decimal SelectedStudentPrepaidHours { get; set; }
        public decimal SelectedStudentTaughtHours { get; set; }
        public decimal SelectedStudentBalance => SelectedStudentPrepaidHours - SelectedStudentTaughtHours;

        public decimal SelectedStudentTotalPaid { get; set; }
        public int SelectedStudentPaymentCount { get; set; }

        public DateTime? SelectedStudentLastPaymentDate { get; set; }
        public decimal SelectedStudentLastPaymentAmount { get; set; }
        public decimal SelectedStudentLastPaymentHours { get; set; }
        public decimal SelectedStudentLastPaymentPricePerHour { get; set; }

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

            TotalPaid = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;

            TotalPrepaidHours = Math.Round(
                await _context.Payments.SumAsync(p => (decimal?)p.HoursPurchased) ?? 0, 0);

            var taughtLessons = await _context.Lessons
                .Where(l => l.IsTaught)
                .ToListAsync();

            TotalTaughtHours = Math.Round(
                taughtLessons.Sum(l =>
                    l.End > l.Start
                        ? (decimal)Math.Round((l.End - l.Start).TotalHours, 0)
                        : l.Hours
                ), 0);

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
                // ⭐ Předplacené hodiny pro studenta (původní vlastnost)
                SelectedStudentPrepaidHours = Math.Round(
                    await _context.Payments
                        .Where(p => p.StudentId == SelectedStudentId)
                        .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0, 0);

                // ⭐ Odučené hodiny pro studenta (původní vlastnost)
                var taughtStudentLessons = await _context.Lessons
                    .Where(l => l.StudentId == SelectedStudentId && l.IsTaught)
                    .ToListAsync();

                SelectedStudentTaughtHours = Math.Round(
                    taughtStudentLessons.Sum(l =>
                        l.End > l.Start
                            ? (decimal)Math.Round((l.End - l.Start).TotalHours, 0)
                            : l.Hours
                    ), 0);

                // ⭐ Celkem zaplaceno
                SelectedStudentTotalPaid = await _context.Payments
                    .Where(p => p.StudentId == SelectedStudentId)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                // ⭐ Počet plateb
                SelectedStudentPaymentCount = await _context.Payments
                    .CountAsync(p => p.StudentId == SelectedStudentId);

                // ⭐ Poslední platba
                var lastPayment = await _context.Payments
                    .Where(p => p.StudentId == SelectedStudentId)
                    .OrderByDescending(p => p.Date)
                    .FirstOrDefaultAsync();

                if (lastPayment != null)
                {
                    SelectedStudentLastPaymentDate = lastPayment.Date;
                    SelectedStudentLastPaymentAmount = lastPayment.Amount;
                    SelectedStudentLastPaymentHours = lastPayment.HoursPurchased;
                    SelectedStudentLastPaymentPricePerHour = lastPayment.PricePerHour ?? 0;
                }

                // ⭐ Napojení na hodnoty pro PDF
                PaymentCount = SelectedStudentPaymentCount;
                PrepaidHours = (int)SelectedStudentPrepaidHours;
                TaughtHours = (int)SelectedStudentTaughtHours;
                LastPricePerHour = SelectedStudentLastPaymentPricePerHour;
            }
        }

        public IActionResult OnGetExportPaymentsPdf(int studentId, DateTime? from, DateTime? to)
        {
            var student = _context.Students.Find(studentId);
            if (student == null)
                return NotFound();

            var payments = _context.Payments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.Date)
                .ToList();

            if (from.HasValue)
                payments = payments.Where(p => p.Date >= from.Value).ToList();

            if (to.HasValue)
                payments = payments.Where(p => p.Date <= to.Value).ToList();

            var dto = payments
                .Select(p => new Vyuka.PDF.PaymentDto
                {
                    Date = p.Date,
                    Amount = p.Amount,
                    HoursPurchased = p.HoursPurchased,
                    Note = p.Note
                })
                .ToList();

            int paymentCount = payments.Count;
            int prepaidHours = (int)payments.Sum(p => p.HoursPurchased);
            decimal lastPricePerHour = payments.Any() ? (payments.First().PricePerHour ?? 0) : 0;

            var taughtLessons = _context.Lessons
                .Where(l => l.StudentId == studentId && l.IsTaught)
                .ToList();

            int taughtHours = (int)Math.Round(
                taughtLessons.Sum(l =>
                    l.End > l.Start
                        ? (decimal)Math.Round((l.End - l.Start).TotalHours, 0)
                        : l.Hours
                ), 0);

            var doc = new Vyuka.PDF.PaymentsReportDocument
            {
                StudentName = $"{student.LastName} {student.FirstName}",
                Payments = dto,
                PaymentCount = paymentCount,
                PrepaidHours = prepaidHours,
                TaughtHours = taughtHours,
                LastPricePerHour = lastPricePerHour
            };

            var pdfBytes = doc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"platby_{student.LastName}_{student.FirstName}.pdf");
        }
    }
}
