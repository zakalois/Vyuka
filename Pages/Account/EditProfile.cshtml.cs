using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Account
{
    public class EditProfileModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public EditProfileModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // ⭐ Input model – aby fungovalo asp-for="Input.*"
        public class InputModel
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public IFormFile? Photo { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string CurrentPhotoPath { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToPage("/Login");

            CurrentPhotoPath = user.PhotoPath;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
                return RedirectToPage("/Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToPage("/Login");

            // ⭐ Uložení textových údajů
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.Email = Input.Email;
            user.UserName = Input.Email;
            user.PhoneNumber = Input.PhoneNumber;

            // ⭐ Uložení fotky
            if (Input.Photo != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(Input.Photo.FileName)}";
                var filePath = Path.Combine("wwwroot/images/users", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.Photo.CopyToAsync(stream);
                }

                user.PhotoPath = $"/images/users/{fileName}";
            }

            await _userManager.UpdateAsync(user);

            return RedirectToPage("/Account/Profile");
        }
    }
}
