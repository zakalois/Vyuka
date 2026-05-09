using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Students_only
{
    public class ProfileModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public ProfileModel(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public string LastLogin { get; set; } = "";

        public async Task OnGetAsync()
        {
            // 1) Získáme ID přihlášeného uživatele
            var userId = _userManager.GetUserId(User);

            // 2) Najdeme studenta podle UserId
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                FullName = "Neznámý student";
                return;
            }

            // 3) Vyplníme data
            FullName = student.FullName;
            Email = student.Email ?? "—";
            Phone = student.Phone ?? "—";
            CreatedAt = student.CreatedAt.ToString("dd.MM.yyyy");
            LastLogin = student.LastLogin?.ToString("dd.MM.yyyy HH:mm") ?? "—";
        }
    }
}
