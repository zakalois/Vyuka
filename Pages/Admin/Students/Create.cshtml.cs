using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Student Student { get; set; } = new();

        [BindProperty]
        public Parent Parent { get; set; } = new();

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

            string tempPassword = GeneratePassword();
            var result = await _userManager.CreateAsync(user, tempPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return Page();
            }

            // 2) Uložení studenta
            Student.UserId = user.Id;

            _context.Students.Add(Student);
            await _context.SaveChangesAsync();   // Student.Id je nyní dostupné

            // 3) Uložení rodiče – správně přes Parent.StudentId
            Parent.StudentId = Student.Id;
            _context.Parents.Add(Parent);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        private string GeneratePassword()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";

            var rand = new Random();

            string password =
                upper[rand.Next(upper.Length)].ToString() +
                lower[rand.Next(lower.Length)].ToString() +
                digits[rand.Next(digits.Length)].ToString() +
                special[rand.Next(special.Length)].ToString();

            string all = upper + lower + digits + special;
            while (password.Length < 10)
                password += all[rand.Next(all.Length)];

            return password;
        }
    }
}
