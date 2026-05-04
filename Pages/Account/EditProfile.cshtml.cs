using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Account
{
    public class EditProfileModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EditProfileModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public class EditProfileInput
        {
            public int Id { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }

            public string Email { get; set; }

            public string Phone { get; set; }

            public IFormFile? Photo { get; set; }
        }

        [BindProperty]
        public EditProfileInput Input { get; set; }

        public string? CurrentPhotoPath { get; set; }

        public IActionResult OnGet()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null)
                return RedirectToPage("/Login");

            var user = _context.AppUsers.FirstOrDefault(u => u.Id == sessionUserId.Value);
            if (user == null)
                return RedirectToPage("/Login");

            Input = new EditProfileInput
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone
            };

            CurrentPhotoPath = user.PhotoPath;

            return Page();
        }

        public IActionResult OnPost()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId == null)
                return RedirectToPage("/Login");

            var user = _context.AppUsers.FirstOrDefault(u => u.Id == sessionUserId.Value);
            if (user == null)
                return RedirectToPage("/Login");

            // Uložení textových údajů
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.Email = Input.Email;
            user.Phone = Input.Phone;

            // Uložení fotky
            if (Input.Photo != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "images/users");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = $"user_{user.Id}{Path.GetExtension(Input.Photo.FileName)}";
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    Input.Photo.CopyTo(stream);
                }

                user.PhotoPath = $"/images/users/{fileName}";
            }

            _context.SaveChanges();

            return RedirectToPage("/Account/Profile");
        }
    }
}
