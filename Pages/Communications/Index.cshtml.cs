using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;
using Microsoft.AspNetCore.Mvc;

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
        public List<Parent> Parents { get; set; } = new();

        // ⭐ Šablony dostupné v komunikaci
        public List<EmailTemplate> Templates { get; set; } = new();

        // ⭐ Email studenta
        public string? SelectedStudentEmail =>
            Students.FirstOrDefault(s => s.Id == SelectedStudentId)?.Email;

        // ⭐ Email rodiče (opraveno)
        public string? SelectedParentEmail =>
            Parents.FirstOrDefault(p => p.StudentId == SelectedStudentId)?.Email;

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

            Parents = await _context.Parents.ToListAsync();
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

        // ---------------------------------------------------------
        // ⭐ PREVIEW
        // ---------------------------------------------------------
        public async Task<IActionResult> OnPostPreviewAsync()
        {
            await LoadStudentsAsync();
            Templates = await _context.EmailTemplates
    .OrderBy(t => t.Name)
    .ToListAsync();


            var student = Students.FirstOrDefault(s => s.Id == SelectedStudentId);
            var parent = Parents.FirstOrDefault(p => p.StudentId == SelectedStudentId);

            if (student == null)
            {
                TempData["Message"] = "Vyberte studenta.";
                return Page();
            }

            Dictionary<string, string> model = new();

            switch (SelectedTemplate)
            {
                case "LessonPlanned":
                    {
                        var plan = await GetNextLessonAsync(student.Id);
                        if (plan == null)
                        {
                            TempData["Message"] = "Tento student nemá žádnou naplánovanou lekci.";
                            return Page();
                        }

                        model = new()
                        {
                            { "StudentName", $"{student.FirstName} {student.LastName}" },
                            { "SubjectName", plan.Subject?.Name ?? "Neuvedeno" },
                            { "LessonDate", plan.Date.ToString("dd.MM.yyyy") },
                            { "LessonTime", plan.Start.ToString(@"hh\\:mm") },
                            { "LessonTopic", plan.SubjectTopic?.Name ?? "" },
                            { "TeacherName", "Alois Učitel" }
                        };
                        break;
                    }

                case "PaymentConfirmation":
                    {
                        var payment = await _context.Payments
                            .Where(p => p.StudentId == student.Id)
                            .OrderByDescending(p => p.Date)
                            .FirstOrDefaultAsync();

                        if (payment == null)
                        {
                            TempData["Message"] = "Tento student nemá žádnou platbu.";
                            return Page();
                        }

                        model = new()
                        {
                            { "StudentName", $"{student.FirstName} {student.LastName}" },
                            { "Amount", payment.Amount.ToString("0.##") },
                            { "PaymentDate", payment.Date.ToString("dd.MM.yyyy") },
                            { "Method", payment.Method ?? "neuvedeno" },
                            { "HoursPurchased", payment.HoursPurchased.ToString("0.##") },
                            { "PricePerHour", payment.PricePerHour?.ToString("0.##") ?? "—" },
                            { "NoteHtml", string.IsNullOrWhiteSpace(payment.Note) ? "" : $"<p><strong>Poznámka:</strong> {payment.Note}</p>" }
                        };
                        break;
                    }

                case "OfferTemplate":
                    {
                        model = new()
                        {
                            { "ParentName", parent?.Name ?? "" },
                            { "StudentName", $"{student.FirstName} {student.LastName}" },
                            { "CustomText", "" }
                        };
                        break;
                    }
            }

            PreviewHtml = _templateService.RenderTemplate(SelectedTemplate, model);
            return Page();
        }

        // ---------------------------------------------------------
        // ⭐ SEND
        // ---------------------------------------------------------
        public async Task<IActionResult> OnPostSendAsync()
        {
            await LoadStudentsAsync();
            Templates = await _context.EmailTemplates
    .OrderBy(t => t.Name)
    .ToListAsync();


            var student = Students.FirstOrDefault(s => s.Id == SelectedStudentId);
            var parent = Parents.FirstOrDefault(p => p.StudentId == SelectedStudentId);

            if (student == null)
            {
                TempData["Message"] = "Vyberte studenta.";
                return Page();
            }

            // ⭐ Výběr adresátů
            List<string> recipients = new();

            if (RecipientType == "student" || RecipientType == "both")
                if (!string.IsNullOrWhiteSpace(student.Email))
                    recipients.Add(student.Email);

            if (RecipientType == "parent" || RecipientType == "both")
                if (!string.IsNullOrWhiteSpace(parent?.Email))
                    recipients.Add(parent.Email);

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
                case "LessonPlanned":
                    {
                        var plan = await GetNextLessonAsync(student.Id);
                        if (plan == null)
                        {
                            TempData["Message"] = "Tento student nemá žádnou naplánovanou lekci.";
                            return Page();
                        }

                        model = new()
                        {
                            { "StudentName", $"{student.FirstName} {student.LastName}" },
                            { "SubjectName", plan.Subject?.Name ?? "Neuvedeno" },
                            { "LessonDate", plan.Date.ToString("dd.MM.yyyy") },
                            { "LessonTime", plan.Start.ToString(@"hh\\:mm") },
                            { "LessonTopic", plan.SubjectTopic?.Name ?? "" },
                            { "TeacherName", "Alois Učitel" }
                        };

                        subject = "Naplánovaná lekce";
                        break;
                    }

                case "PaymentConfirmation":
                    {
                        var payment = await _context.Payments
                            .Where(p => p.StudentId == student.Id)
                            .OrderByDescending(p => p.Date)
                            .FirstOrDefaultAsync();

                        if (payment == null)
                        {
                            TempData["Message"] = "Tento student nemá žádnou platbu.";
                            return Page();
                        }

                        model = new()
                        {
                            { "StudentName", $"{student.FirstName} {student.LastName}" },
                            { "Amount", payment.Amount.ToString("0.##") },
                            { "PaymentDate", payment.Date.ToString("dd.MM.yyyy") },
                            { "Method", payment.Method ?? "neuvedeno" },
                            { "HoursPurchased", payment.HoursPurchased.ToString("0.##") },
                            { "PricePerHour", payment.PricePerHour?.ToString("0.##") ?? "—" },
                            { "NoteHtml", string.IsNullOrWhiteSpace(payment.Note) ? "" : $"<p><strong>Poznámka:</strong> {payment.Note}</p>" }
                        };

                        subject = "Potvrzení platby";
                        break;
                    }

                case "OfferTemplate":
                    {
                        model = new()
                        {
                            { "ParentName", parent?.Name ?? "" },
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
