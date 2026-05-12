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

        [BindProperty]
        public Parent Parent { get; set; }

        public List<Lesson> Lessons { get; set; } = new();
        public List<LessonPlan> Plans { get; set; } = new();

        public List<Subject> Subjects { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return RedirectToPage("/Index");

            // ⭐ Student bez Parent
            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == teacher.Id);

            if (Student == null)
                return RedirectToPage("/Teachers_only/Students/Index");

            // ⭐ Načíst rodiče správně
            Parent = await _context.Parents
                .FirstOrDefaultAsync(p => p.StudentId == Student.Id)
                ?? new Parent();

            // ⭐ Předměty učitele
            Subjects = await _context.Subjects
                .Where(s => s.TeacherId == teacher.Id)
                .OrderBy(s => s.Name)
                .ToListAsync();

            Lessons = await _context.Lessons
                .Where(l => l.StudentId == id)
                .OrderByDescending(l => l.Date)
                .ToListAsync();

            Plans = await _context.LessonPlans
                .Where(lp => lp.StudentId == id)
                .OrderBy(lp => lp.Date)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return RedirectToPage("/Index");

            var studentDb = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == teacher.Id);

            if (studentDb == null)
                return RedirectToPage("/Teachers_only/Students/Index");

            // ⭐ Učitel může upravit jen bezpečná pole
            studentDb.Email = Student.Email;
            studentDb.Phone = Student.Phone;
            studentDb.Note = Student.Note;
            studentDb.SubjectId = Student.SubjectId;
            studentDb.Level = Student.Level;
            studentDb.PreferredTime = Student.PreferredTime;

            // ⭐ Rodič – načíst nebo vytvořit
            var parentDb = await _context.Parents
                .FirstOrDefaultAsync(p => p.StudentId == studentDb.Id);

            if (parentDb == null)
            {
                parentDb = new Parent
                {
                    StudentId = studentDb.Id
                };
                _context.Parents.Add(parentDb);
            }

            parentDb.Name = Parent.Name;
            parentDb.Email = Parent.Email;
            parentDb.Phone = Parent.Phone;
            parentDb.Note = Parent.Note;

            await _context.SaveChangesAsync();

            return RedirectToPage("/Teachers_only/Students/Index");
        }
    }
}
