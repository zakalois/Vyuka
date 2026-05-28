using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Communications
{
    public class LogsDetailModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _email;

        public LogsDetailModel(AppDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        public EmailLog Log { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Log = await _context.EmailLogs.FirstOrDefaultAsync(l => l.Id == id);

            if (Log == null)
                return NotFound();

            return Page();
        }

        // ⭐ Znovu odeslat e‑mail
        public async Task<IActionResult> OnPostResendAsync(int id)
        {
            var log = await _context.EmailLogs.FirstOrDefaultAsync(l => l.Id == id);
            if (log == null)
                return NotFound();

            await _email.SendAsync(
                log.Recipient,
                log.Subject,
                log.Html,
                null,
                null,
                null,
                null,
                log.StudentName,
                log.EmailType,
                log.StudentId
            );

            TempData["Message"] = "E‑mail byl znovu odeslán.";
            return RedirectToPage(new { id });
        }
    }
}
