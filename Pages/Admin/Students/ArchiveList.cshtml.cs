using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class ArchiveListModel : PageModel
    {
        private readonly AppDbContext _context;

        public ArchiveListModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Student> Students { get; set; } = new();

        public async Task OnGetAsync()
        {
            Students = await _context.Students
                .Where(s => s.ArchivedAt != null)          // ⭐ jen archivovaní
                .Include(s => s.Subject)
                .OrderByDescending(s => s.ArchivedAt)      // ⭐ nejnovější nahoře
                .ToListAsync();
        }
    }
}
