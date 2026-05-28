using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Communications
{
    public class LogsModel : PageModel
    {
        private readonly AppDbContext _context;

        public LogsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<EmailLog> Logs { get; set; } = new();
        public List<Student> Students { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? Type { get; set; }
        [BindProperty(SupportsGet = true)] public int? StudentId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }

        public async Task OnGetAsync()
        {
            Students = await _context.Students
                .OrderBy(s => s.LastName)
                .ToListAsync();

            var query = _context.EmailLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Type))
                query = query.Where(l => l.EmailType == Type);

            if (StudentId.HasValue)
                query = query.Where(l => l.StudentId == StudentId);

            if (From.HasValue)
                query = query.Where(l => l.SentAt >= From.Value);

            if (To.HasValue)
                query = query.Where(l => l.SentAt <= To.Value.AddDays(1));

            Logs = await query
                .OrderByDescending(l => l.SentAt)
                .Take(300)
                .ToListAsync();
        }

        // ⭐ Automatické čištění starých logů (např. starších než 6 měsíců)
        public async Task<IActionResult> OnPostCleanAsync()
        {
            var limit = DateTime.Now.AddMonths(-6);

            var oldLogs = _context.EmailLogs.Where(l => l.SentAt < limit);
            _context.EmailLogs.RemoveRange(oldLogs);

            await _context.SaveChangesAsync();

            TempData["Message"] = "Staré logy byly odstraněny.";
            return RedirectToPage();
        }
    }
}
