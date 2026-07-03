using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Communications
{
    public class SendTeachingOfferModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public SendTeachingOfferModel(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty] public int? SelectedStudentId { get; set; }
        [BindProperty] public string? Subject { get; set; }
        [BindProperty] public string? Message { get; set; }
        [BindProperty] public string? Amount { get; set; }   // ⭐ cena
        [BindProperty] public string? PaymentMessage { get; set; } // ⭐ zpráva pro příjemce
        [BindProperty] public List<IFormFile>? Attachments { get; set; }

        public List<SelectListItem> StudentList { get; set; } = new();
        public string? PreviewHtml { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadStudents();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadStudents();

            if (!SelectedStudentId.HasValue)
            {
                TempData["Error"] = "Musíte vybrat studenta.";
                return Page();
            }

            var student = await _context.Students
                .Include(s => s.Lessons)
                .FirstOrDefaultAsync(s => s.Id == SelectedStudentId.Value);

            if (student == null)
            {
                TempData["Error"] = "Student nebyl nalezen.";
                return Page();
            }

            var lastLesson = student.Lessons
                .Where(l => l.IsTaught)
                .OrderByDescending(l => l.Date)
                .FirstOrDefault();

            PreviewHtml = BuildPreview(student, lastLesson);

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
                await _emailService.SendAsync(
      !string.IsNullOrWhiteSpace(student.ParentEmail)
          ? student.ParentEmail
          : student.Email,

      Subject ?? "Nabídka výuky",
    PreviewHtml,
    emailAttachments,
    decimal.TryParse(Amount, out var amountValue) ? amountValue : null,   // dynamicAmount
    PaymentMessage,                                                       // dynamicMessage
    Message,                                                              // customText
    student.FullName,                                                     // studentName
    "offer",                                                              // emailType
    student.Id                                                            // studentId
);


                TempData["Success"] = "E‑mail byl odeslán.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "E‑mail se nepodařilo odeslat: " + ex.Message;
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

        private string BuildPreview(Student student, Lesson? lastLesson)
        {
            // ⭐ Bezpečné sestavení jména rodiče
            string parentName = "";

            if (!string.IsNullOrWhiteSpace(student.ParentFirstName) ||
                !string.IsNullOrWhiteSpace(student.ParentLastName))
            {
                parentName = $"{student.ParentFirstName} {student.ParentLastName}".Trim();
            }

            // ⭐ Bezpečné jméno studenta
            string studentName = !string.IsNullOrWhiteSpace(student.FullName)
                ? student.FullName
                : "student";

            string html = @"
<div style=""font-family: 'Segoe UI', sans-serif; background: #f7f9fc; color: #1e293b; padding: 20px; line-height: 1.6;"">
    <div style=""background: white; padding: 28px; border-radius: 12px; max-width: 650px; margin: auto; box-shadow: 0 2px 6px rgba(0,0,0,0.08);"">

        <h2 style=""color: #b45309; margin-top: 0; text-align: left;"">Nabídka online výuky</h2>

        <p>Dobrý den{{ParentNamePart}},</p>

        <p>Děkujeme Vám za zájem o online výuku. Rádi bychom nabídli individuální lekce pro <strong>{{StudentName}}</strong>.</p>

        <p>Výuka je zaměřena na technické předměty, zejména <strong>Matematika, Fyzika, Informatika, Elektrotechnika</strong> a další.</p>
        
        <p>Samotná výuka probíhá formou videohovoru přes <strong>Google Meet</strong>, který je zdarma a snadno dostupný.</p>

        <p>Lekce jsou vedeny individuálně, s důrazem na pochopení látky a praktické procvičení.</p>

        <p>Termín výuky přizpůsobíme Vašim časovým možnostem.</p>

        <p>V nejbližší době Vás kontaktujeme telefonicky, abychom domluvili úvodní hodinu a zodpověděli případné dotazy.</p>

        <div style=""font-weight: 700; margin-top: 24px; margin-bottom: 8px; color: #b45309;"">
            Cena výuky a platební údaje
        </div>

        <p>
            Cena: <strong>{{Amount}} Kč</strong><br>
            Zpráva pro příjemce: <strong>{{PaymentMessage}}</strong>
        </p>

        <p>Pro rychlou platbu použijte QR kód níže:</p>

        <div style=""margin-top:20px; text-align:left;"">
            <img src=""cid:qrDynamic"" alt=""QR platba"" width=""180"" height=""180""
                 style=""display:block; border:1px solid #b45309; border-radius:4px; padding:4px;"">
        </div>

        <p style=""margin-top: 20px;"">
            Pokud nechcete využít QR kód, můžete platbu provést také převodem na účet:<br>
            <strong>2349690015 / 3030 (Air Bank)</strong>
        </p>

    </div>
</div>";

            // ⭐ Oslovení – buď „Dobrý den Nováková,“ nebo jen „Dobrý den,“
            if (string.IsNullOrWhiteSpace(parentName))
            {
                html = html.Replace("{{ParentNamePart}}", "");
            }
            else
            {
                html = html.Replace("{{ParentNamePart}}", " " + parentName);
            }

            html = html.Replace("{{StudentName}}", studentName);
            html = html.Replace("{{Amount}}", Amount ?? "0");
            html = html.Replace("{{PaymentMessage}}", PaymentMessage ?? "");
            html = html.Replace("{{CustomText}}", Message ?? "");

            return html;
        }





    }
}
