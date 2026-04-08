using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Vyuka.Models;

namespace Vyuka.Pages.Subjects
{
    public class SubjectsIndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public SubjectsIndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Subject> Subjects { get; set; } = new List<Subject>();

        public async Task OnGetAsync()
        {
            Subjects = await _context.Subjects
                .AsNoTracking()
                .ToListAsync();

            Subjects = Subjects
                .OrderBy(s => s.Name, StringComparer.Create(new CultureInfo("cs-CZ"), true))
                .ToList();
        }
    }
}