using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using Microsoft.AspNetCore.Identity;

namespace Vyuka.Pages.Admin.Teachers
{
    public class CreateModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = new AppUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                PhoneNumber = Input.Phone,
                Role = "Teacher"
            };

            // heslo zatím natvrdo – později uděláme generování a email
            var result = await _userManager.CreateAsync(user, "Teacher123!");

            if (result.Succeeded)
            {
                return RedirectToPage("/Admin/Teachers/Teachers");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return Page();
        }
    }
}
