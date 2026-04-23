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
        private readonly TemplateService _templateService;
        private readonly IEmailService _emailService;

        public IndexModel(AppDbContext context, TemplateService templateService, IEmailService emailService)
        {
            _context = context;
            _templateService = templateService;
            _emailService = emailService;
        }

        public List<Student> Students { get; set; } = new();

        public List<string> Templates { get; set; } = new()
        {
            "LessonPlanned",
            "PaymentConfirmation"
        };

        public string PreviewHtml { get; set; } = "";

        [BindProperty]
        public int SelectedStudentId { get; set; }

        [BindProperty]
        public string SelectedTemplate { get; set; }

        [BindProperty]
        public string RecipientType { get; set; } = "student";

        private async Task LoadStudentsAsync()
        {
            Students = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.LastName)
                .ToListAsync();
        }

        public async Task OnGetAsync()
        {
            await LoadStudentsAsync();
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
        // 🔥 PREVIEW E-MAILU
        // ---------------------------------------------------------
        public async Task<IActionResult> OnPostPreviewAsync()
        {
            await LoadStudentsAsync();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == SelectedStudentId);

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
                        var plan = await GetNextLessonAsync(SelectedStudentId);

                        if (plan == null)
                        {
                            TempData["Message"] = "Tento student nemá žádnou naplánovanou lekci.";
                            return Page();
                        }

                        string lessonDate = plan.Date.ToString("dd.MM.yyyy");
                        string lessonTime = plan.Start.ToString(@"hh\:mm");

                        model = new()
                    {
                        { "StudentName", $"{student.FirstName} {student.LastName}" },
                        { "SubjectName", plan.Subject?.Name ?? "Neuvedeno" },
                        { "LessonDate", lessonDate },
                        { "LessonTime", lessonTime },
                        { "LessonTopic", plan.SubjectTopic?.Name ?? "" },
                        { "TeacherName", "Alois Učitel" },
                        { "SenderName", "Alois" }
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

                        string noteHtml = string.IsNullOrWhiteSpace(payment.Note)
                            ? ""
                            : $"<p><strong>Poznámka:</strong> {payment.Note}</p>";

                        model = new()
                    {
                        { "StudentName", $"{student.FirstName} {student.LastName}" },
                        { "Amount", payment.Amount.ToString("0.##") },
                        { "PaymentDate", payment.Date.ToString("dd.MM.yyyy") },
                        { "Method", payment.Method ?? "neuvedeno" },
                        { "HoursPurchased", payment.HoursPurchased.ToString("0.##") },
                        { "PricePerHour", payment.PricePerHour?.ToString("0.##") ?? "—" },
                        { "NoteHtml", noteHtml }
                    };

                        break;
                    }
            }

            PreviewHtml = _templateService.RenderTemplate(SelectedTemplate, model);
            return Page();
        }

        // ---------------------------------------------------------
        // 🔥 ODESLÁNÍ E-MAILU (student / rodič / oba)
        // ---------------------------------------------------------
        public async Task<IActionResult> OnPostSendAsync()
        {
            await LoadStudentsAsync();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == SelectedStudentId);

            if (student == null)
            {
                TempData["Message"] = "Vyberte studenta.";
                return Page();
            }

            // ⭐ Výběr adresátů
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
                TempData["Message"] = "Není dostupný žádný e‑mail pro odeslání.";
                return Page();
            }

            Dictionary<string, string> model = new();
            string subject = "";

            switch (SelectedTemplate)
            {
                case "LessonPlanned":
                    {
                        var plan = await GetNextLessonAsync(SelectedStudentId);

                        if (plan == null)
                        {
                            TempData["Message"] = "Tento student nemá žádnou naplánovanou lekci.";
                            return Page();
                        }

                        string lessonDate = plan.Date.ToString("dd.MM.yyyy");
                        string lessonTime = plan.Start.ToString(@"hh\:mm");

                        model = new()
                    {
                        { "StudentName", $"{student.FirstName} {student.LastName}" },
                        { "SubjectName", plan.Subject?.Name ?? "Neuvedeno" },
                        { "LessonDate", lessonDate },
                        { "LessonTime", lessonTime },
                        { "LessonTopic", plan.SubjectTopic?.Name ?? "" },
                        { "TeacherName", "Alois Učitel" },
                        { "SenderName", "Alois" }
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

                        string noteHtml = string.IsNullOrWhiteSpace(payment.Note)
                            ? ""
                            : $"<p><strong>Poznámka:</strong> {payment.Note}</p>";

                        model = new()
                    {
                        { "StudentName", $"{student.FirstName} {student.LastName}" },
                        { "Amount", payment.Amount.ToString("0.##") },
                        { "PaymentDate", payment.Date.ToString("dd.MM.yyyy") },
                        { "Method", payment.Method ?? "neuvedeno" },
                        { "HoursPurchased", payment.HoursPurchased.ToString("0.##") },
                        { "PricePerHour", payment.PricePerHour?.ToString("0.##") ?? "—" },
                        { "NoteHtml", noteHtml }
                    };

                        subject = "Potvrzení platby";
                        break;
                    }
            }

            string html = _templateService.RenderTemplate(SelectedTemplate, model);

            try
            {
                foreach (var email in recipients)
                {
                    await _emailService.SendAsync(email, subject, html);
                }

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