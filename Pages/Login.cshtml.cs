using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using Vyuka.Secrets;

namespace Vyuka.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vyplňte email i heslo.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);

            if (user == null)
            {
                ErrorMessage = "Nesprávný email nebo heslo.";
                return Page();
            }

            // Odhlásíme předchozího uživatele
            await _signInManager.SignOutAsync();

            // Přihlásíme správného uživatele
            var result = await _signInManager.PasswordSignInAsync(
                user,
                Password,
                isPersistent: false,
                lockoutOnFailure: false
            );


            if (!result.Succeeded)
            {
                ErrorMessage = "Nesprávný email nebo heslo.";
                return Page();
            }

            // Redirect podle role
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                return RedirectToPage("/Admin/Dashboard");

            if (await _userManager.IsInRoleAsync(user, Roles.Teacher))
                return RedirectToPage("/Teachers_only/Dashboard");

            if (await _userManager.IsInRoleAsync(user, Roles.Student))
                return RedirectToPage("/Students_only/Dashboard");


            return RedirectToPage("/Index");
        }
    }
}
