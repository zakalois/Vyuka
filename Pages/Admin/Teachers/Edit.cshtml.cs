using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Microsoft.AspNetCore.Identity;

namespace Vyuka.Pages.Admin.Teachers
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public EditModel(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            // AspNetUsers
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var teacher = await _db.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            Input = new InputModel
            {
                Id = teacher.Id,
                FirstName = teacher.User.FirstName,
                LastName = teacher.User.LastName,
                Email = teacher.User.Email,
                Phone = teacher.User.PhoneNumber
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var teacher = await _db.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == Input.Id);

            if (teacher == null)
                return NotFound();

            // Uložíme do AspNetUsers
            teacher.User.FirstName = Input.FirstName;
            teacher.User.LastName = Input.LastName;
            teacher.User.Email = Input.Email;
            teacher.User.UserName = Input.Email;
            teacher.User.PhoneNumber = Input.Phone;

            await _db.SaveChangesAsync();

            return RedirectToPage("/Admin/Teachers/Teachers");
        }
    }
}
