using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Communications
{
    public class IndexModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly ITemplateService _templateService;

        public IndexModel(IEmailService emailService, AppDbContext context, ITemplateService templateService)
        {
            _emailService = emailService;
            _context = context;
            _templateService = templateService;
        }

        public List<Student> Students { get; set; } = new();

        [BindProperty]
        public int SelectedStudentId { get; set; }

        [BindProperty]
        public string RecipientType { get; set; } = "student";

        [BindProperty]
        public string SelectedTemplate { get; set; } = "LessonPlanned.html";

        [BindProperty]
        public string SubjectName { get; set; }

        public List<string> Templates { get; set; } = new()
        {
            "LessonPlanned.html"
        };

        public string? PreviewHtml { get; set; }

        public async Task OnGetAsync()
        {
            await LoadStudentsAsync();
        }

        private async Task LoadStudentsAsync()
        {
            Students = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        // 🔵 PREVIEW HANDLER
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

            // 🔥 NAČTENÍ PŘEDMĚTU Z ROZVRHU
            var lesson = await _context.Lessons
                .Include(l => l.Subject)
                .FirstOrDefaultAsync(l => l.StudentId == SelectedStudentId);

            SubjectName = lesson?.Subject?.Name ?? "Neuvedeno";

            PreviewHtml = _templateService.RenderTemplate(SelectedTemplate, new Dictionary<string, string>
            {
                { "StudentName", $"{student.FirstName} {student.LastName}" },
                { "LessonDate", DateTime.Now.ToString("dd.MM.yyyy") },
                { "LessonTime", "14:00" },
                { "TeacherName", "Alois Učitel" },
                { "SenderName", "Alois" },
                { "SubjectName", SubjectName }
            });

            return Page();
        }

        // 🔵 SEND HANDLER
        public async Task<IActionResult> OnPostSendAsync()
        {
            await LoadStudentsAsync();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == SelectedStudentId);

            if (student == null)
            {
                TempData["Message"] = "Student nebyl nalezen.";
                return Page();
            }

            string? email = RecipientType switch
            {
                "student" => student.Email,
                "parent" => student.ParentEmail,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Message"] = "Vybraný adresát nemá vyplněný e‑mail.";
                return Page();
            }

            // 🔥 NAČTENÍ PŘEDMĚTU Z ROZVRHU (STEJNÉ JAKO V PREVIEW)
            var lesson = await _context.Lessons
                .Include(l => l.Subject)
                .FirstOrDefaultAsync(l => l.StudentId == SelectedStudentId);

            SubjectName = lesson?.Subject?.Name ?? "Neuvedeno";

            var html = _templateService.RenderTemplate(SelectedTemplate, new Dictionary<string, string>
            {
                { "StudentName", $"{student.FirstName} {student.LastName}" },
                { "LessonDate", DateTime.Now.ToString("dd.MM.yyyy") },
                { "LessonTime", "14:00" },
                { "TeacherName", "Alois Učitel" },
                { "SenderName", "Alois" },
                { "SubjectName", SubjectName }
            });

            await _emailService.SendEmailAsync(
                email,
                "Naplánovaná lekce",
                html
            );

            TempData["Message"] = $"E‑mail byl odeslán na {email}.";
            return RedirectToPage();
        }
    }
}