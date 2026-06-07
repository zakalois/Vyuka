using Microsoft.AspNetCore.Mvc;
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

        public List<StudentOverviewDto> Students { get; set; } = new();
        public List<Student> AllStudents { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Filter { get; set; }

        public async Task OnGetAsync()
        {
            // Dropdown – všichni studenti (jen aktivní, nearchivovaní)
            AllStudents = await _context.Students
                .Where(s => s.IsActive && s.ArchivedAt == null)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // Základní dotaz – vždy ignorujeme archivované
            var query = _context.Students
                .Include(s => s.Subject)
                .Where(s => s.ArchivedAt == null)   // ⭐ TADY JE KLÍČOVÁ OPRAVA
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .AsQueryable();

            // Filtr
            switch (Filter)
            {
                case "active":
                    query = query.Where(s => s.IsActive);
                    break;

                case "inactive":
                    query = query.Where(s => !s.IsActive);
                    break;
            }

            var students = await query.ToListAsync();

            // Načteme všechny lekce a platby najednou (rychlé)
            var lessons = await _context.Lessons
                .Where(l => l.IsTaught)
                .ToListAsync();

            var payments = await _context.Payments.ToListAsync();

            // Sestavení DTO
            Students = students.Select(s =>
            {
                var paid = payments
                    .Where(p => p.StudentId == s.Id)
                    .Sum(p => (double)p.HoursPurchased);

                var taught = lessons
                    .Where(l => l.StudentId == s.Id)
                    .Sum(l =>
                        l.End > l.Start
                            ? (l.End - l.Start).TotalMinutes / 60.0
                            : (double)l.Hours
                    );

                return new StudentOverviewDto
                {
                    Id = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Subject = s.Subject.Name,

                    ParentFirstName = s.ParentFirstName,
                    ParentLastName = s.ParentLastName,
                    ParentEmail = s.ParentEmail,
                    ParentPhone = s.ParentPhone,

                    IsActive = s.IsActive,

                    PaidHours = paid,
                    TaughtHours = taught,
                    RemainingHours = paid - taught
                };
            })
            .ToList();
        }

    }

    // DTO pro přehled studentů
    public class StudentOverviewDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public string Subject { get; set; } = "";

        public string? ParentFirstName { get; set; }
        public string? ParentLastName { get; set; }
        public string? ParentEmail { get; set; }
        public string? ParentPhone { get; set; }

        public bool IsActive { get; set; }

        public double PaidHours { get; set; }
        public double TaughtHours { get; set; }
        public double RemainingHours { get; set; }
    }
}
