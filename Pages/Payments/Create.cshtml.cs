using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Payments
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _email;
        private readonly IWebHostEnvironment _env;

        public CreateModel(AppDbContext context, IEmailService email, IWebHostEnvironment env)
        {
            _context = context;
            _email = email;
            _env = env;
        }

        public SelectList StudentList { get; set; } = default!;

        [BindProperty] public int SelectedStudentId { get; set; }
        [BindProperty] public DateTime Date { get; set; } = DateTime.Today;
        [BindProperty] public decimal Amount { get; set; }
        [BindProperty] public decimal HoursPurchased { get; set; }
        [BindProperty] public decimal? PricePerHour { get; set; }
        [BindProperty] public string? Method { get; set; }
        [BindProperty] public string? Note { get; set; }

        public async Task OnGetAsync()
        {
            // ⭐ Anti‑duplicate token – ochrana proti opakovanému odeslání
            TempData["PaymentToken"] = Guid.NewGuid().ToString();

            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            StudentList = new SelectList(
                students.Select(s => new
                {
                    Id = s.Id,
                    FullName = $"{s.LastName} {s.FirstName}"
                }),
                "Id",
                "FullName"
            );
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // ⭐ 1) Anti‑duplicate token kontrola
            var token = Request.Form["PaymentToken"].ToString();

            if (string.IsNullOrEmpty(token))
                return RedirectToPage("/Payments/Index");

            if (TempData[token] != null)
                return RedirectToPage("/Payments/Index");

            TempData[token] = "used";

            // ⭐ 2) Validace
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            // ⭐ 3) Silnější ochrana proti duplicitám
            var exists = await _context.Payments.AnyAsync(p =>
                p.StudentId == SelectedStudentId &&
                p.Amount == Amount &&
                p.Date == Date &&
                EF.Functions.DateDiffMinute(p.Date, DateTime.Now) < 5
            );

            if (exists)
                return RedirectToPage("/Payments/Index");

            // ⭐ 4) Uložit platbu
            var payment = new Payment
            {
                StudentId = SelectedStudentId,
                Date = Date,
                Amount = Amount,
                HoursPurchased = HoursPurchased,
                PricePerHour = PricePerHour,
                Method = Method,
                Note = Note
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // ⭐ 5) Načíst studenta
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == SelectedStudentId);

            if (student == null)
            {
                ModelState.AddModelError("", "Student neexistuje.");
                return Page();
            }

            // ⭐ 6) Email – výběr příjemce
            string? emailToSend =
                !string.IsNullOrWhiteSpace(student.ParentEmail) ? student.ParentEmail :
                !string.IsNullOrWhiteSpace(student.Email) ? student.Email :
                "zaka@outlook.cz";

            // ⭐ 7) Načíst HTML šablonu
            var templatePath = Path.Combine(_env.ContentRootPath, "EmailsTemplates", "PaymentConfirmation.html");
            var html = await System.IO.File.ReadAllTextAsync(templatePath);

            html = html.Replace("{{StudentName}}", $"{student.FirstName} {student.LastName}")
                       .Replace("{{Amount}}", Amount.ToString("0.##"))
                       .Replace("{{PaymentDate}}", Date.ToString("dd.MM.yyyy"))
                       .Replace("{{Method}}", Method ?? "neuvedeno")
                       .Replace("{{HoursPurchased}}", HoursPurchased.ToString("0.#"))
                       .Replace("{{PricePerHour}}", PricePerHour?.ToString("0.##") ?? "neuvedeno")
                       .Replace("{{NoteHtml}}", string.IsNullOrWhiteSpace(Note)
                            ? ""
                            : $"<p><strong>Poznámka:</strong> {Note}</p>");

            // ⭐ 8) Odeslat e-mail
            await _email.SendAsync(
                emailToSend,
                "Potvrzení platby",
                html,
                null,
                Amount,
                null,
                null,
                $"{student.FirstName} {student.LastName}",
                "payment",
                student.Id
            );

            // ⭐ 9) Hotovo
            return RedirectToPage("/Payments/Index");
        }
    }
}
