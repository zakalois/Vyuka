using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Teachers_only.Notes
{
    [Authorize(Roles = "Teacher")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Student> Students { get; set; }
        public List<Note> Notes { get; set; }

        [BindProperty] public int? SelectedStudentId { get; set; }
        [BindProperty] public string NewNoteText { get; set; }

        public Student SelectedStudent { get; set; }

        // GET
        public void OnGet(int? studentId)
        {
            SelectedStudentId = studentId;

            LoadStudents();
            LoadSelectedStudent();
            LoadNotes();
        }

        // POST – změna studenta
        public IActionResult OnPostSelect()
        {
            return RedirectToPage(new { studentId = SelectedStudentId });
        }

        // POST – přidání poznámky
        public async Task<IActionResult> OnPostAddNote()
        {
            if (SelectedStudentId == null)
                return RedirectToPage();

            var user = await _userManager.GetUserAsync(User);

            var teacher = _context.Teachers
                .FirstOrDefault(t => t.UserId == user.Id);

            if (teacher == null)
                return RedirectToPage(new { studentId = SelectedStudentId });

            var note = new Note
            {
                StudentId = SelectedStudentId,
                TeacherId = teacher.Id,
                Text = NewNoteText,
                CreatedBy = user.Name,
                Created = DateTime.Now
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            // ⭐ ZŮSTAT NA STEJNÉM STUDENTOVI
            return RedirectToPage(new { studentId = SelectedStudentId });
        }

        private void LoadStudents()
        {
            Students = _context.Students
                .OrderBy(s => s.LastName)
                .ToList();
        }

        private void LoadSelectedStudent()
        {
            if (SelectedStudentId == null)
            {
                SelectedStudent = null;
                return;
            }

            SelectedStudent = _context.Students
                .FirstOrDefault(s => s.Id == SelectedStudentId);
        }

        private void LoadNotes()
        {
            if (SelectedStudentId == null)
            {
                Notes = new();
                return;
            }

            Notes = _context.Notes
                .Include(n => n.Teacher).ThenInclude(t => t.User)
                .Where(n => n.StudentId == SelectedStudentId)
                .OrderByDescending(n => n.Created)
                .ToList();
        }
    }
}
