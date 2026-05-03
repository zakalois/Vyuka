using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.StudentPortal
{
    public class ProfileModel : PageModel
    {
        private readonly AppDbContext _db;

        public ProfileModel(AppDbContext db)
        {
            _db = db;
        }

        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public string LastLogin { get; set; } = "";

        public async Task OnGetAsync()
        {
            int studentId = 1;

            var student = await _db.Students.FindAsync(studentId);

            FullName = student.FullName;
            Email = student.Email ?? "—";
            Phone = student.Phone ?? "—";
            CreatedAt = student.CreatedAt.ToString("dd.MM.yyyy");
            LastLogin = student.LastLogin?.ToString("dd.MM.yyyy HH:mm") ?? "—";
        }
    }
}
