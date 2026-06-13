using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Students_only
{
    [Authorize(Roles = "Student")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public DashboardModel(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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

        public async Task<IActionResult> OnGetAsync()
        {
            // 1) Získáme ID přihlášeného uživatele
            var userId = _userManager.GetUserId(User);

            // 2) Najdeme studenta podle UserId
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            // ⭐ 3) Kontrola aktivace – NEAKTIVNÍ STUDENT NESMÍ DOVNITŘ
            if (student == null || !student.IsActive)
            {
                return RedirectToPage("/Students_only/RegistrationPending");
            }

            // ⭐ 4) Student je aktivní → pokračujeme dál
            StudentName = student.FullName;

            // 5) Zůstatek hodin
            var prepaid = await _db.Payments
                .Where(p => p.StudentId == student.Id)
                .SumAsync(p => (decimal?)p.HoursPurchased) ?? 0;

            var taught = await _db.Lessons
                .Where(l => l.StudentId == student.Id)
                .SumAsync(l => (decimal?)l.Hours) ?? 0;

            FormattedBalance = (prepaid - taught).ToString("0.0");

            // 6) Nejbližší lekce
            var next = await _db.LessonPlans
                .Include(l => l.Subject)
                .Where(l => l.StudentId == student.Id && l.Date >= DateTime.Today)
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

            // 7) Poslední platba
            var lastPayment = await _db.Payments
                .Where(p => p.StudentId == student.Id)
                .OrderByDescending(p => p.Date)
                .FirstOrDefaultAsync();

            if (lastPayment != null)
            {
                LastPaymentDate = lastPayment.Date.ToString("dd.MM.yyyy");
                LastPaymentAmount = lastPayment.Amount.ToString("N0");
            }

            return Page();
        }
    }
}
