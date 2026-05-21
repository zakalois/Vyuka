using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class OverviewModel : PageModel
    {
        private readonly AppDbContext _context;

        public OverviewModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Student> AllStudents { get; set; } = new();
        public Student? SelectedStudent { get; set; }

        public async Task OnGetAsync(int? id)
        {
            // 🔹 Načteme všechny studenty pro dropdown
            AllStudents = await _context.Students
                .OrderBy(s => s.LastName)
                .ToListAsync();

            if (id != null)
            {
                // ⭐ Načíst studenta i s Teacher a Subject
                SelectedStudent = await _context.Students
                    .Include(s => s.Teacher)
                    .Include(s => s.Subject)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (SelectedStudent != null)
                {
                    // ⭐ SPRÁVNÉ PÁROVÁNÍ: Teacher.UserId (GUID) ↔ Student.UserId (GUID)
                    if (!string.IsNullOrEmpty(SelectedStudent.UserId))
                    {
                        SelectedStudent.Teacher = await _context.Teachers
                            .FirstOrDefaultAsync(t => t.UserId == SelectedStudent.UserId);
                    }

                    SelectedStudent.TaughtHours = await _context.Lessons
                        .Where(l => l.StudentId == id && l.IsTaught)
                        .SumAsync(l => (double)l.Hours);

                    SelectedStudent.PaidHours = await _context.Payments
                        .Where(p => p.StudentId == id)
                        .SumAsync(p => (double)p.HoursPurchased);

                    SelectedStudent.RemainingHours =
                        SelectedStudent.PaidHours - SelectedStudent.TaughtHours;
                }
            }
        }
    }
}
