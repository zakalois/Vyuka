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

        public IList<StudentWithParent> Students { get; set; } = new List<StudentWithParent>();

        public async Task OnGetAsync()
        {
            var students = await _context.Students
                .Include(s => s.Teacher)
                    .ThenInclude(t => t.User)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            var lessons = await _context.Lessons.Where(l => l.IsTaught).ToListAsync();
            var payments = await _context.Payments.ToListAsync();

            foreach (var s in students)
            {
             
                // Odučené hodiny
                double taught = lessons
                    .Where(l => l.StudentId == s.Id)
                    .Sum(l => (l.End - l.Start).TotalHours);

                // Předplacené hodiny
                double paid = (double)payments
    .Where(p => p.StudentId == s.Id)
    .Sum(p => p.HoursPurchased);


                Students.Add(new StudentWithParent
                {
                    Student = s,
             
                });

                s.TaughtHours = Math.Round(taught, 1);
                s.PaidHours = Math.Round(paid, 1);
                s.RemainingHours = Math.Round(s.PaidHours - s.TaughtHours, 1);
            }
        }
    }

    public class StudentWithParent
    {
        public Student Student { get; set; }
        public Parent Parent { get; set; }
    }
}
