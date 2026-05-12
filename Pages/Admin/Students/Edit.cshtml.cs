using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class SubjectSelection
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; }
    }

    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Student Student { get; set; } = new();

        [BindProperty]
        public Parent Parent { get; set; } = new();

        [BindProperty]
        public List<SubjectSelection> Subjects { get; set; } = new();

        // ⭐ ON GET — načtení studenta + rodiče + předmětů
        public async Task<IActionResult> OnGetAsync(int id)
        {
            // načteme studenta BEZ Parent
            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (Student == null)
                return NotFound();

            // načteme rodiče přes Parent.StudentId
            Parent = await _context.Parents
                .FirstOrDefaultAsync(p => p.StudentId == Student.Id);

            // pokud rodič neexistuje, vytvoříme prázdný objekt
            if (Parent == null)
                Parent = new Parent();

            return Page();
        }


        // ⭐ ON POST — uložení studenta + rodiče + předmětů
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // 1) Načteme studenta BEZ Parent
            var studentDb = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == Student.Id);

            if (studentDb == null)
                return NotFound();

            // 2) Načteme rodiče přes Parent.StudentId
            var parentDb = await _context.Parents
                .FirstOrDefaultAsync(p => p.StudentId == studentDb.Id);

            // 3) Aktualizace studenta
            studentDb.FirstName = Student.FirstName;
            studentDb.LastName = Student.LastName;
            studentDb.Age = Student.Age;
            studentDb.Email = Student.Email;
            studentDb.Phone = Student.Phone;
            studentDb.School = Student.School;
            studentDb.Address = Student.Address;
            studentDb.Level = Student.Level;
            studentDb.PreferredTime = Student.PreferredTime;
            studentDb.Note = Student.Note;
            studentDb.TeacherId = Student.TeacherId;
            studentDb.SubjectId = Student.SubjectId;
            studentDb.Credit = Student.Credit;

            _context.Students.Update(studentDb);

            // 4) Aktualizace nebo vytvoření rodiče
            if (parentDb != null)
            {
                parentDb.Name = Parent.Name;
                parentDb.Email = Parent.Email;
                parentDb.Phone = Parent.Phone;
                parentDb.Note = Parent.Note;

                _context.Parents.Update(parentDb);
            }
            else
            {
                parentDb = new Parent
                {
                    StudentId = studentDb.Id,
                    Name = Parent.Name,
                    Email = Parent.Email,
                    Phone = Parent.Phone,
                    Note = Parent.Note
                };

                _context.Parents.Add(parentDb);
            }

            // 5) Uložit změny
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

    }
}
