using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    [Authorize(Roles = "Admin,Teacher")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<StudentWithParent> Students { get; set; } = new List<StudentWithParent>();

        public async Task OnGetAsync()
        {
            // Základní dotaz
            var query = _context.Students
                .Include(s => s.Teacher)
                    .ThenInclude(t => t.User)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .AsQueryable();

            // ⭐ UČITEL → vidí jen své studenty
            if (User.IsInRole("Teacher"))
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

                if (teacher != null)
                {
                    query = query.Where(s => s.TeacherId == teacher.Id);
                }
            }

            var students = await query.ToListAsync();

            // Načtení hodin a plateb
            var lessons = await _context.Lessons.Where(l => l.IsTaught).ToListAsync();
            var payments = await _context.Payments.ToListAsync();

            foreach (var s in students)
            {
                // Odučené hodiny
                double taught = lessons
                    .Where(l => l.StudentId == s.Id)
                    .Sum(l => (l.End - l.Start).TotalHours);

                // Předplacené hodiny (decimal → double)
                decimal paidDec = payments
                    .Where(p => p.StudentId == s.Id)
                    .Sum(p => p.HoursPurchased);

                double paid = (double)paidDec;

                // Výsledky
                s.TaughtHours = Math.Round(taught, 1);
                s.PaidHours = Math.Round(paid, 1);
                s.RemainingHours = Math.Round(s.PaidHours - s.TaughtHours, 1);

                // Přidání do seznamu
                Students.Add(new StudentWithParent
                {
                    Student = s,
                    Parent = null // připraveno pro budoucí Parent join
                });

            }
        }
    }

    public class StudentWithParent
    {
        public Student Student { get; set; }
        public Parent Parent { get; set; }
    }
}
