using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Communications
{
    public class BulkEmailModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public BulkEmailModel(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public int? TemplateId { get; set; }

        public List<EmailTemplate> Templates { get; set; }

        [BindProperty]
        public string Subject { get; set; } = string.Empty;

        [BindProperty]
        public string Body { get; set; } = string.Empty;

        [BindProperty]
        public decimal? Amount { get; set; }

        [BindProperty]
        public string PaymentMessage { get; set; }

        [BindProperty]
        public string RecipientType { get; set; } = "Parents";

        [BindProperty]
        public List<EmailRecipient> Recipients { get; set; }

        public int RecipientsCount { get; set; }

        public class EmailRecipient
        {
            public int Id { get; set; }
            public string StudentEmail { get; set; }
            public string ParentEmail { get; set; }
            public string FullName { get; set; }
            public bool Selected { get; set; }
        }

        private void LoadRecipients()
        {
            Recipients = _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new EmailRecipient
                {
                    Id = s.Id,
                    StudentEmail = s.Email,
                    ParentEmail = s.ParentEmail,
                    FullName = s.LastName + " " + s.FirstName,
                    Selected = false
                })
                .ToList();

            RecipientsCount = Recipients.Count;
        }

        public string GetDisplayEmail(EmailRecipient r, string type)
        {
            return type switch
            {
                "Parents" => r.ParentEmail,
                "Students" => r.StudentEmail,
                "Both" => !string.IsNullOrEmpty(r.StudentEmail)
                            ? r.StudentEmail
                            : r.ParentEmail,
                _ => r.StudentEmail
            };
        }

        public void OnGet()
        {
            Templates = _context.EmailTemplates.OrderBy(t => t.Name).ToList();
            LoadRecipients();
        }

        public IActionResult OnPostLoadTemplate()
        {
            Templates = _context.EmailTemplates.OrderBy(t => t.Name).ToList();

            if (TemplateId.HasValue)
            {
                var template = _context.EmailTemplates.FirstOrDefault(t => t.Id == TemplateId.Value);
                if (template != null)
                {
                    Subject = template.Subject;
                    Body = template.Body;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSend()
        {
            if (Recipients == null || Recipients.Count == 0)
                LoadRecipients();

            var selected = Recipients.Where(r => r.Selected).ToList();

            if (!selected.Any())
            {
                Templates = _context.EmailTemplates.OrderBy(t => t.Name).ToList();
                LoadRecipients();
                TempData["Success"] = "Nebyl vybrán žádný příjemce.";
                return Page();
            }

            int sentCount = 0;

            foreach (var r in selected)
            {
                var emails = new List<string>();

                switch (RecipientType)
                {
                    case "Parents":
                        if (!string.IsNullOrEmpty(r.ParentEmail))
                            emails.Add(r.ParentEmail);
                        break;

                    case "Students":
                        if (!string.IsNullOrEmpty(r.StudentEmail))
                            emails.Add(r.StudentEmail);
                        break;

                    case "Both":
                        if (!string.IsNullOrEmpty(r.ParentEmail))
                            emails.Add(r.ParentEmail);
                        if (!string.IsNullOrEmpty(r.StudentEmail))
                            emails.Add(r.StudentEmail);
                        break;
                }

                emails = emails.Distinct().ToList();

                foreach (var email in emails)
                {
                    var personalizedBody = Body
                        .Replace("{{FullName}}", r.FullName)
                        .Replace("{{Amount}}", Amount?.ToString("0") ?? "")
                        .Replace("{{Message}}", PaymentMessage ?? "");

                    await _emailService.SendAsync(
                        to: email,
                        subject: Subject,
                        html: personalizedBody,
                        attachments: null,
                        dynamicAmount: Amount,
                        dynamicMessage: PaymentMessage,
                        customText: null,
                        studentName: r.FullName,
                        emailType: "Bulk",
                        studentId: r.Id
                    );

                    sentCount++;
                }
            }

            TempData["Success"] = $"Odesláno {sentCount} e‑mailů.";

            Templates = _context.EmailTemplates.OrderBy(t => t.Name).ToList();
            LoadRecipients();

            return Page();
        }
    }
}
