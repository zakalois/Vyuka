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

        [BindProperty]
        public int SelectedStudentId { get; set; }

        [BindProperty]
        public DateTime Date { get; set; } = DateTime.Today;

        [BindProperty]
        public decimal Amount { get; set; }

        [BindProperty]
        public decimal HoursPurchased { get; set; }

        [BindProperty]
        public decimal? PricePerHour { get; set; }

        [BindProperty]
        public string? Method { get; set; }

        [BindProperty]
        public string? Note { get; set; }

        public async Task OnGetAsync()
        {
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
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            // 1) Uložit platbu
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

            // 2) Načíst studenta
            var student = await _context.Students.FindAsync(SelectedStudentId);

            // 3) Načíst HTML šablonu
            var templatePath = Path.Combine(_env.ContentRootPath, "EmailsTemplates", "PaymentConfirmation.html");
            var html = await System.IO.File.ReadAllTextAsync(templatePath);

            // 4) Nahradit placeholdery
            html = html.Replace("{{StudentName}}", $"{student.FirstName} {student.LastName}")
                       .Replace("{{Amount}}", Amount.ToString("0.##"))
                       .Replace("{{PaymentDate}}", Date.ToString("dd.MM.yyyy"))
                       .Replace("{{Method}}", Method ?? "neuvedeno")
                       .Replace("{{HoursPurchased}}", HoursPurchased.ToString("0.#"))
                       .Replace("{{PricePerHour}}", PricePerHour?.ToString("0.##") ?? "neuvedeno")
                       .Replace("{{NoteHtml}}", string.IsNullOrWhiteSpace(Note)
                            ? ""
                            : $"<p><strong>Poznámka:</strong> {Note}</p>");

            // 5) Odeslat e‑mail
            await _email.SendAsync(student.Email, "Potvrzení platby", html);

            // 6) Přesměrování
            return RedirectToPage("/Payments/Index");
        }
    }
}
