using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Teachers_only.Students
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
        public int SubjectId { get; set; }

        public List<Subject> Subjects { get; set; } = new();

        public async Task OnGet()
        {
            Subjects = await _context.Subjects.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Subjects = await _context.Subjects.ToListAsync();
                return Page();
            }

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

                Subjects = await _context.Subjects.ToListAsync();
                return Page();
            }

            // 2) Automatické přiřazení TeacherId
            var currentUser = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (teacher != null)
                Student.TeacherId = teacher.Id;

            // 3) Uložení studenta
            Student.UserId = user.Id;
            Student.SubjectId = SubjectId;

            _context.Students.Add(Student);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Teachers_only/Students/Index");
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
