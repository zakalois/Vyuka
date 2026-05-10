using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Student> Students { get; set; } = new List<Student>();

        // StudentId → Odučené hodiny
        public Dictionary<int, double> TaughtHours { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Načteme studenty i s učiteli
            Students = await _context.Students
    .Include(s => s.Teacher)
    .OrderBy(s => s.LastName)
    .ThenBy(s => s.FirstName)
    .ToListAsync();


            // Načteme všechny odučené lekce
            var lessons = await _context.Lessons
                .Where(l => l.IsTaught)
                .ToListAsync();

            // Načteme všechny payments
            var payments = await _context.Payments.ToListAsync();

            // Pro každý student spočítáme hodiny
            foreach (var s in Students)
            {
                // ODUČENÉ HODINY
                double taught = lessons
                    .Where(l => l.StudentId == s.Id)
                    .Sum(l => (l.End - l.Start).TotalHours);

                s.TaughtHours = Math.Round(taught, 1);

                // PŘEDPLACENÉ HODINY
                double paid = (double)payments
     .Where(p => p.StudentId == s.Id)
     .Sum(p => p.HoursPurchased);


                s.PaidHours = Math.Round(paid, 1);

                // ZBÝVAJÍCÍ HODINY
                s.RemainingHours = Math.Round(s.PaidHours - s.TaughtHours, 1);
            }

        }
    }
}



