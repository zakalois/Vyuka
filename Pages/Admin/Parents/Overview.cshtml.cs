using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Parents
{
    public class OverviewModel : PageModel
    {
        private readonly AppDbContext _context;

        public OverviewModel(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Všichni rodiče (každý student = jeden rodič)
        public List<Student> AllParents { get; set; } = new();

        // 🔹 Vybraný rodič (Student objekt)
        public Student? SelectedParent { get; set; }

        // 🔹 Hledaný výraz
        public string? Search { get; set; }

        public async Task OnGetAsync(int? id, string? search)
        {
            Search = search;

            // Základní dotaz – studenti, kteří mají nějaké rodičovské údaje
            var query = _context.Students
                .Where(s =>
                    !string.IsNullOrEmpty(s.ParentFirstName) ||
                    !string.IsNullOrEmpty(s.ParentLastName) ||
                    !string.IsNullOrEmpty(s.ParentEmail))
                .AsQueryable();

            // 🔍 Vyhledávání rodičů
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.ParentFirstName.Contains(search) ||
                    s.ParentLastName.Contains(search) ||
                    s.ParentEmail.Contains(search));
            }

            // Výsledný seznam rodičů
            AllParents = await query
                .OrderBy(s => s.ParentLastName)
                .ToListAsync();

            // Vybraný rodič
            if (id != null)
            {
                SelectedParent = await _context.Students
                    .Include(s => s.Teacher)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
        }
    }
}
