using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Teachers_only.Parents
{
    public class DetailModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailModel(AppDbContext context)
        {
            _context = context;
        }

        public Student? Student { get; set; }

        public string? ParentName { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentEmail { get; set; }

        public async Task<IActionResult> OnGetAsync(int studentId)
        {
            Student = await _context.Students
                .Include(s => s.Subject)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (Student == null)
                return NotFound();

            ParentName = $"{Student.ParentLastName} {Student.ParentFirstName}".Trim();
            ParentPhone = Student.ParentPhone;
            ParentEmail = Student.ParentEmail;

            return Page();
        }
    }
}
