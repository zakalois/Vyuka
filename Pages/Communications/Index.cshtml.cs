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

        public IndexModel(
            AppDbContext context,
            ITemplateService templateService,
            IEmailService emailService)
        {
            _context = context;
            _templateService = templateService;
            _emailService = emailService;
        }

        public List<Student> Students { get; set; } = new();
        public List<EmailTemplate> Templates { get; set; } = new();

        [BindProperty] public int SelectedStudentId { get; set; }
        [BindProperty] public string SelectedTemplate { get; set; } = "";
        [BindProperty] public string RecipientType { get; set; } = "student";

        // ⭐ Dynamický QR
        [BindProperty] public decimal PaymentAmount { get; set; } = 1750;
        [BindProperty] public string PaymentMessage { get; set; } = "Balíček 5 hodin";

        public string PreviewHtml { get; set; } = "";

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
            Templates = await _context.EmailTemplates.OrderBy(t => t.Name).ToListAsync();
        }

        // ⭐ Náhled
        public async Task<IActionResult> OnPostPreviewAsync()
        {
            await LoadStudentsAsync();
            Templates = await _context.EmailTemplates.OrderBy(t => t.Name).ToListAsync();

            var student = Students.FirstOrDefault(s => s.Id == SelectedStudentId);
            if (student == null)
            {
                TempData["Message"] = "Vyberte studenta.";
                return Page();
            }

            string html = _templateService.RenderTemplate(SelectedTemplate, new());

            html = html.Replace("{{StudentName}}", $"{student.FirstName} {student.LastName}");
            html = html.Replace("{{Amount}}", PaymentAmount.ToString());
            html = html.Replace("{{Message}}", PaymentMessage);
            html = html.Replace("{{CustomText}}", "");

            PreviewHtml = html;
            return Page();
        }

        // ⭐ Odeslání e‑mailu
        public async Task<IActionResult> OnPostSendAsync()
        {
            await LoadStudentsAsync();
            Templates = await _context.EmailTemplates.OrderBy(t => t.Name).ToListAsync();

            var student = Students.FirstOrDefault(s => s.Id == SelectedStudentId);
            if (student == null)
            {
                TempData["Message"] = "Vyberte studenta.";
                return Page();
            }

            List<string> recipients = new();

            if (RecipientType == "student" || RecipientType == "both")
            {
                if (!string.IsNullOrWhiteSpace(student.Email))
                    recipients.Add(student.Email);
            }

            if (RecipientType == "parent" || RecipientType == "both")
            {
                if (!string.IsNullOrWhiteSpace(student.ParentEmail))
                    recipients.Add(student.ParentEmail);
            }

            if (recipients.Count == 0)
            {
                TempData["Message"] = "Není dostupný žádný e‑mail pro zvoleného příjemce.";
                return Page();
            }

            string html = _templateService.RenderTemplate(SelectedTemplate, new());

            html = html.Replace("{{StudentName}}", $"{student.FirstName} {student.LastName}");
            html = html.Replace("{{Amount}}", PaymentAmount.ToString());
            html = html.Replace("{{Message}}", PaymentMessage);
            html = html.Replace("{{CustomText}}", "");

            foreach (var email in recipients)
            {
                string subject = "Učitel Žák – automatická zpráva";

                await _emailService.SendAsync(
                    email,
                    subject,
                    html,
                    null,
                    PaymentAmount,
                    PaymentMessage,
                    "",
                    $"{student.FirstName} {student.LastName}"
                );

            }

            TempData["Message"] = "E‑mail byl odeslán.";
            return Page();
        }
    }
}
