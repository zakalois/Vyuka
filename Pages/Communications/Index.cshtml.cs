using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
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

        [BindProperty] public decimal PaymentAmount { get; set; }
        [BindProperty] public string PaymentMessage { get; set; }


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

        private async Task<string> BuildEmailHtml(Student student)
        {
            string html = _templateService.RenderTemplate(SelectedTemplate, new());

            var lastLesson = await _context.Lessons
                .Where(l => l.StudentId == student.Id)
                .OrderByDescending(l => l.Date)
                .FirstOrDefaultAsync();

            string lessonDate = lastLesson?.Date.ToString("dd.MM.yyyy") ?? "";
            string lessonTime = lastLesson?.Date.ToString("HH:mm") ?? "";

            html = html.Replace("{{StudentName}}", $"{student.FirstName} {student.LastName}");
            html = html.Replace("{{Amount}}", PaymentAmount.ToString());
            html = html.Replace("{{Message}}", PaymentMessage);
            html = html.Replace("{{CustomText}}", "");

            html = html.Replace("{{LessonDate}}", lessonDate);
            html = html.Replace("{{LessonTime}}", lessonTime);

            // QR kód se generuje až v EmailService → náhled bude prázdný
            html = html.Replace("{{QrCode}}", "");

            return html;
        }

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

            PreviewHtml = await BuildEmailHtml(student);
            return Page();
        }

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

            string html = await BuildEmailHtml(student);

            foreach (var email in recipients)
            {
                string subject = "Učitel Žák – automatická zpráva";

                await _emailService.SendAsync(
    email,                          // to
    subject,                        // subject
    html,                           // html
    null,                           // attachments (zatím nepoužíváš)
    PaymentAmount,                  // dynamicAmount
    PaymentMessage,                 // dynamicMessage
    null,                           // customText
    $"{student.FirstName} {student.LastName}",   // studentName
    "payment",                      // emailType
    student.Id                      // studentId
);


            }

            TempData["Message"] = "E‑mail byl odeslán.";
            return Page();
        }
    }
}
