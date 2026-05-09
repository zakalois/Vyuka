using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Students_only
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _db;

        public DashboardModel(AppDbContext db)
        {
            _db = db;
        }

        public string StudentName { get; set; } = "";
        public string FormattedBalance { get; set; } = "0.0";

        public LessonRow? NextLesson { get; set; }

        public string LastPaymentDate { get; set; } = "—";
        public string LastPaymentAmount { get; set; } = "0";

        public class LessonRow
        {
            public DateTime Date { get; set; }
            public string Subject { get; set; } = "";
            public decimal Hours { get; set; }
        }

        public async Task OnGetAsync()
        {
            int studentId = 1; // TODO: nahradit přihlášeným studentem

            var student = await _db.Students.FindAsync(studentId);
            StudentName = student.FullName;

            var prepaid = await _db.Payments
                .Where(p => p.StudentId == studentId)
                .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0;

            var taught = await _db.Lessons
                .Where(l => l.StudentId == studentId)
                .SumAsync(l => (decimal?)l.Hours) ?? 0;

            FormattedBalance = (prepaid - taught).ToString("0.0");

            var next = await _db.LessonPlans
                .Include(l => l.Subject)
                .Where(l => l.StudentId == studentId && l.Date > DateTime.Now)
                .OrderBy(l => l.Date)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                NextLesson = new LessonRow
                {
                    Date = next.Date,
                    Subject = next.Subject.Name,
                    Hours = Math.Round((decimal)(next.End - next.Start).TotalHours, 1)
                };
            }

            var lastPayment = await _db.Payments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.Date)
                .FirstOrDefaultAsync();

            if (lastPayment != null)
            {
                LastPaymentDate = lastPayment.Date.ToString("dd.MM.yyyy");
                LastPaymentAmount = lastPayment.Amount.ToString("N0");
            }
        }
    }
}
