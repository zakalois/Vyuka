using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Identity.UI.Services;   // ← zakomentováno
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        // private readonly IEmailSender _emailSender;   // ← zakomentováno

        public CreateModel(AppDbContext context, UserManager<AppUser> userManager /*, IEmailSender emailSender*/)
        {
            _context = context;
            _userManager = userManager;
            // _emailSender = emailSender;   // ← zakomentováno
        }

        [BindProperty]
        public Student Student { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // 1) Vytvoření Identity účtu
            var user = new AppUser
            {
                UserName = Student.Email,
                Email = Student.Email,
                FirstName = Student.FirstName,
                LastName = Student.LastName
            };

            string tempPassword = "Student123!";

            var result = await _userManager.CreateAsync(user, tempPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return Page();
            }

            // 2) Uložení studenta do Students tabulky
            Student.UserId = user.Id;
            _context.Students.Add(Student);
            await _context.SaveChangesAsync();

            // 3) Odeslání e-mailu studentovi (zatím vypnuto)
            /*
            await _emailSender.SendEmailAsync(
                Student.Email,
                "Váš účet byl vytvořen",
                $"Dobrý den {Student.FirstName},<br>" +
                $"váš účet byl vytvořen.<br><br>" +
                $"Přihlaste se pomocí e-mailu: <b>{Student.Email}</b><br>" +
                $"Dočasné heslo: <b>{tempPassword}</b><br><br>" +
                $"Po přihlášení si heslo změňte.");
            */

            return RedirectToPage("Index");
        }
    }
}
