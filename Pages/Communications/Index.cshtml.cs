using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Communications
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ITemplateService _templateService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public IndexModel(
            AppDbContext context,
            ITemplateService templateService,
            IEmailService emailService,
            IWebHostEnvironment env)
        {
            _context = context;
            _templateService = templateService;
            _emailService = emailService;
            _env = env;
        }

        public List<Student> Students { get; set; } = new();
        public List<EmailTemplate> Templates { get; set; } = new();

        public string? SelectedStudentEmail =>
            Students.FirstOrDefault(s => s.Id == SelectedStudentId)?.Email;

        public string? SelectedParentEmail =>
            Students.FirstOrDefault(s => s.Id == SelectedStudentId)?.ParentEmail;

        public string PreviewHtml { get; set; } = "";

        [BindProperty] public int SelectedStudentId { get; set; }
        [BindProperty] public string SelectedTemplate { get; set; } = "";
        [BindProperty] public string RecipientType { get; set; } = "student";

        private async Task LoadStudentsAsync()
        {
            Students = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task OnGetAsync()
        {
            await LoadStudentsAsync();

            Templates = await _context.EmailTemplates
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        private async Task<LessonPlan?> GetNextLessonAsync(int studentId)
        {
            return await _context.LessonPlans
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .Where(p => p.StudentId == studentId && p.Date >= DateTime.Today)
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Start)
                .FirstOrDefaultAsync();
        }

        public async Task<IActionResult> OnPostPreviewAsync()
        {
            await LoadStudentsAsync();
            Templates = await _context.EmailTemplates
                .OrderBy(t => t.Name)
                .ToListAsync();

            var student = Students.FirstOrDefault(s => s.Id == SelectedStudentId);

            if (student == null)
            {
                TempData["Message"] = "Vyberte studenta.";
                return Page();
            }

            Dictionary<string, string> model = new();

            switch (SelectedTemplate)
            {
                case "OfferTemplate":
                    model = new()
                    {
                        { "ParentName", $"{student.ParentFirstName} {student.ParentLastName}" },
                        { "StudentName", $"{student.FirstName} {student.LastName}" },
                        { "CustomText", "" }
                    };
                    break;
            }

            PreviewHtml = _templateService.RenderTemplate(SelectedTemplate, model);
            return Page();
        }

        public async Task<IActionResult> OnPostSendAsync()
        {
            await LoadStudentsAsync();
            Templates = await _context.EmailTemplates
                .OrderBy(t => t.Name)
                .ToListAsync();

            var student = Students.FirstOrDefault(s => s.Id == SelectedStudentId);

            if (student == null)
            {
                TempData["Message"] = "Vyberte studenta.";
                return Page();
            }

            List<string> recipients = new();

            if (RecipientType == "student" || RecipientType == "both")
                if (!string.IsNullOrWhiteSpace(student.Email))
                    recipients.Add(student.Email);

            if (RecipientType == "parent" || RecipientType == "both")
                if (!string.IsNullOrWhiteSpace(student.ParentEmail))
                    recipients.Add(student.ParentEmail);

            if (recipients.Count == 0)
            {
                TempData["Message"] = "Není dostupný žádný e‑mail pro odeslání.";
                return Page();
            }

            Dictionary<string, string> model = new();
            string subject = "";
            List<EmailAttachment>? attachments = null;

            switch (SelectedTemplate)
            {
                case "OfferTemplate":
                    model = new()
                    {
                        { "ParentName", $"{student.ParentFirstName} {student.ParentLastName}" },
                        { "StudentName", $"{student.FirstName} {student.LastName}" },
                        { "CustomText", "" }
                    };

                    subject = "Nabídka online výuky";

                    attachments = new()
                    {
                        new EmailAttachment("qr1", Path.Combine(_env.WebRootPath, "images/QR/1_hod_400.jpg")),
                        new EmailAttachment("qr2", Path.Combine(_env.WebRootPath, "images/QR/10_hod_3500.jpg"))
                    };
                    break;
            }

            string html = _templateService.RenderTemplate(SelectedTemplate, model);

            try
            {
                foreach (var email in recipients)
                    await _emailService.SendAsync(email, subject, html, attachments);

                TempData["Message"] = $"E‑mail byl odeslán na: {string.Join(", ", recipients)}.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Chyba při odesílání e‑mailu: " + ex.Message;
            }

            return Page();
        }
    }
}
