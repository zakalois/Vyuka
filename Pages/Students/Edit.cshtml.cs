using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Students
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
        public List<SubjectSelection> Subjects { get; set; } = new();

        // ⭐ ON GET — načtení studenta + jeho předmětů
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Student = await _context.Students.FindAsync(id);

            if (Student == null)
                return NotFound();

            // načteme všechny předměty
            var allSubjects = await _context.Subjects.ToListAsync();

            // načteme předměty, které student má
            var studentSubjects = await _context.StudentSubjects
                .Where(ss => ss.StudentId == id)
                .Select(ss => ss.SubjectId)
                .ToListAsync();

            // vytvoříme seznam checkboxů
            Subjects = allSubjects
                .Select(s => new SubjectSelection
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsSelected = studentSubjects.Contains(s.Id)
                })
                .ToList();

            return Page();
        }

        // ⭐ ON POST — uložení studenta + jeho předmětů
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // uložíme změny studenta
            _context.Attach(Student).State = EntityState.Modified;

            // smažeme staré vazby
            var existing = _context.StudentSubjects
                .Where(ss => ss.StudentId == Student.Id);
            _context.StudentSubjects.RemoveRange(existing);

            // přidáme nové vazby
            foreach (var subj in Subjects.Where(s => s.IsSelected))
            {
                _context.StudentSubjects.Add(new StudentSubject
                {
                    StudentId = Student.Id,
                    SubjectId = subj.Id
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}