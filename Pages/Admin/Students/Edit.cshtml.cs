using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Student Student { get; set; } = new();

        // ⭐ ON GET — načtení studenta
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (Student == null)
                return NotFound();

            return Page();
        }

        // ⭐ ON POST — uložení studenta
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var studentDb = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == Student.Id);

            if (studentDb == null)
                return NotFound();

            // 🔹 Aktualizace studenta
            studentDb.FirstName = Student.FirstName;
            studentDb.LastName = Student.LastName;
            studentDb.Age = Student.Age;
            studentDb.Email = Student.Email;
            studentDb.Phone = Student.Phone;
            studentDb.School = Student.School;
            studentDb.Address = Student.Address;

            // 🔹 Rodič
            studentDb.ParentFirstName = Student.ParentFirstName;
            studentDb.ParentLastName = Student.ParentLastName;
            studentDb.ParentEmail = Student.ParentEmail;
            studentDb.ParentPhone = Student.ParentPhone;

            // 🔹 Poznámka
            studentDb.Note = Student.Note;

            // 🔹 Úroveň a preferovaný čas
            studentDb.Level = Student.Level;
            studentDb.PreferredTime = Student.PreferredTime;

            _context.Students.Update(studentDb);
            await _context.SaveChangesAsync();

            // 🔹 Návrat na kartu studenta
            return RedirectToPage("/Admin/Students/Overview", new { id = Student.Id });
        }
    }
}
