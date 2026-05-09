using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Vyuka.Models;

namespace Vyuka.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public ChangePasswordModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public ChangePasswordInput Input { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public class ChangePasswordInput
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToPage("/Login");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Login");

            // 1) Ověření nového hesla
            if (Input.NewPassword != Input.ConfirmPassword)
            {
                ErrorMessage = "Nové heslo a potvrzení hesla se neshodují.";
                return Page();
            }

            // 2) Pokus o změnu hesla přes Identity
            var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);

            if (!result.Succeeded)
            {
                ErrorMessage = "Současné heslo není správné.";
                return Page();
            }

            // 3) Refresh přihlášení
            await _signInManager.RefreshSignInAsync(user);

            SuccessMessage = "Heslo bylo úspěšně změněno.";
            return Page();
        }

    }
}
