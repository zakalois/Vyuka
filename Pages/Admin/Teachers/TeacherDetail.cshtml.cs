using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Teachers
{
    public class TeacherDetailModel : PageModel
    {
        private readonly AppDbContext _db;

        public TeacherDetailModel(AppDbContext db)
        {
            _db = db;
        }

        public Teacher Teacher { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Teacher = await _db.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Teacher == null)
                return NotFound();

            return Page();
        }
    }
}
