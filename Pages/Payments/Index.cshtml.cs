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

        public SelectList StudentList { get; set; } = default!;
        public List<Payment> Payments { get; set; } = new();

        // ⭐ Nové vlastnosti
        public decimal TotalPurchasedHours { get; set; }
        public decimal TotalTaughtHours { get; set; }
        public decimal BalanceHours { get; set; }

        [BindProperty(SupportsGet = true)]
        public int SelectedStudentId { get; set; }

        public async Task OnGetAsync()
        {
            // Načteme studenty
            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // Dropdown studentů – jméno + příjmení
            StudentList = new SelectList(
                students.Select(s => new
                {
                    Id = s.Id,
                    FullName = $"{s.LastName} {s.FirstName}"
                }),
                "Id",
                "FullName"
            );

            // Pokud je vybraný student, načteme jeho platby
            if (SelectedStudentId > 0)
            {
                Payments = await _context.Payments
                    .Where(p => p.StudentId == SelectedStudentId)
                    .OrderByDescending(p => p.Date)
                    .ToListAsync();

                // Předplacené hodiny
                TotalPurchasedHours = Payments.Sum(p => p.HoursPurchased);

                // Odučené hodiny
                TotalTaughtHours = await _context.Lessons
                    .Where(l => l.StudentId == SelectedStudentId && l.IsTaught)
                    .SumAsync(l => l.Hours);

                // Zůstatek
                BalanceHours = TotalPurchasedHours - TotalTaughtHours;
            }
        }
    }
}