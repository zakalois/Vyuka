using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using System.Threading.Tasks;
using System.IO;

namespace Vyuka.Pages.Account
{
    [Authorize]
    public class EditProfileModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public EditProfileModel(UserManager<AppUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        public string CurrentPhotoPath { get; set; }

        // ⭐ INPUT MODEL – správně!
        public class InputModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }

            public IFormFile PhotoFile { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            CurrentPhotoPath = user.PhotoPath;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            // ⭐ Uložení textových údajů
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.Email = Input.Email;
            user.PhoneNumber = Input.PhoneNumber;

            // ⭐ Uložení fotky
            if (Input.PhotoFile != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "images/users");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = $"user_{user.Id}{Path.GetExtension(Input.PhotoFile.FileName)}";
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.PhotoFile.CopyToAsync(stream);
                }

                user.PhotoPath = $"/images/users/{fileName}";
            }

            await _userManager.UpdateAsync(user);

            return RedirectToPage("/Account/Profile");
        }
    }
}
