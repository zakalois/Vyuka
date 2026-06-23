using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Communications
{
    public class SendOfferModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public SendOfferModel(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ⭐ Form fields
        [BindProperty] public int? SelectedStudentId { get; set; }
        [BindProperty] public string? Subject { get; set; }
        [BindProperty] public string? Message { get; set; }
        [BindProperty] public List<IFormFile>? Attachments { get; set; }

        // ⭐ Dropdown
        public List<SelectListItem> StudentList { get; set; } = new();

        // ⭐ Preview
        public string? PreviewHtml { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadStudents();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadStudents();

            // ❗ Student musí být vybrán
            if (!SelectedStudentId.HasValue)
            {
                TempData["Error"] = "Musíte vybrat studenta.";
                return Page();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == SelectedStudentId.Value);

            if (student == null)
            {
                TempData["Error"] = "Student nebyl nalezen.";
                return Page();
            }

            // ⭐ Vytvoření HTML náhledu – jen tvůj text
            PreviewHtml = BuildPreview();

            // ⭐ Zpracování příloh
            List<EmailAttachment>? emailAttachments = null;

            if (Attachments != null && Attachments.Any())
            {
                emailAttachments = new List<EmailAttachment>();

                foreach (var file in Attachments)
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    emailAttachments.Add(new EmailAttachment
                    {
                        FileName = file.FileName,
                        Content = ms.ToArray(),
                        ContentType = file.ContentType
                    });
                }
            }

            try
            {
                // ⭐ Odeslání e‑mailu
                await _emailService.SendAsync(
                    student.Email ?? "",
                    Subject ?? "",
                    PreviewHtml,
                    emailAttachments
                );

                TempData["Success"] = "E‑mail byl odeslán.";
                return RedirectToPage();
            }
            catch
            {
                TempData["Error"] = "E‑mail se nepodařilo odeslat.";
                return Page();
            }
        }

        private async Task LoadStudents()
        {
            StudentList = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.LastName)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.FullName
                })
                .ToListAsync();
        }

        // ⭐ ČISTÝ PREVIEW – jen text od tebe
        private string BuildPreview()
        {
            return $@"
<div style=""font-family:Segoe UI,Arial,sans-serif; font-size:15px; color:#333;"">
    {Message}
</div>";
        }
    }
}
