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

        public List<Student> Students { get; set; } = new();
        public List<Student> AllStudents { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Filter { get; set; }

        public async Task OnGetAsync()
        {
            // Dropdown – všichni studenti
            AllStudents = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            // Tabulka – filtrovaní studenti
            var query = _context.Students
                .Include(s => s.Subject)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .AsQueryable();

            switch (Filter)
            {
                case "active":
                    query = query.Where(s => s.IsActive);
                    break;

                case "inactive":
                    query = query.Where(s => !s.IsActive);
                    break;
            }

            Students = await query.ToListAsync();
        }

    }
}
