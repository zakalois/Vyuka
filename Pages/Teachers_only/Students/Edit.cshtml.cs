using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vyuka.Models;

namespace Vyuka.Pages.Teachers_only.Students
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Student Student { get; set; }

        public List<Lesson> Lessons { get; set; } = new();
        public List<LessonPlan> Plans { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Přihlášený učitel
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return RedirectToPage("/Index");

            // ⭐ Student patřící tomuto učiteli
            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == teacher.UserId);

            if (Student == null)
                return RedirectToPage("/Teachers_only/Students/Index");


            // ⭐ Předměty učitele
            Subjects = await _context.Subjects
                .Where(s => s.TeacherId == teacher.Id)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // ⭐ Lekce studenta
            Lessons = await _context.Lessons
                .Where(l => l.StudentId == id)
                .OrderByDescending(l => l.Date)
                .ToListAsync();

            // ⭐ Plány lekcí
            Plans = await _context.LessonPlans
                .Where(lp => lp.StudentId == id)
                .OrderBy(lp => lp.Date)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            // Přihlášený učitel
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return RedirectToPage("/Index");
            // ⭐ Student patřící tomuto učiteli
            var studentDb = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == teacher.UserId);

            if (studentDb == null)
                return RedirectToPage("/Teachers_only/Students/Index");


            // ⭐ Učitel může upravit jen bezpečná pole
            studentDb.Email = Student.Email;
            studentDb.Phone = Student.Phone;
            studentDb.Note = Student.Note;
            studentDb.SubjectId = Student.SubjectId;
            studentDb.Level = Student.Level;
            studentDb.PreferredTime = Student.PreferredTime;

            // ⭐ Rodič – ukládá se přímo do Student
            studentDb.ParentFirstName = Student.ParentFirstName;
            studentDb.ParentLastName = Student.ParentLastName;
            studentDb.ParentEmail = Student.ParentEmail;
            studentDb.ParentPhone = Student.ParentPhone;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Teachers_only/Students/Index");
        }
    }
}
