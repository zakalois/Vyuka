using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using System.ComponentModel.DataAnnotations;


public class RegisterModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public RegisterModel(UserManager<AppUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [BindProperty]
    public RegisterInputModel Input { get; set; }

    public class RegisterInputModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
    }


    public async Task<IActionResult> OnPostAsync()
    {
        var user = new AppUser
        {
            UserName = Input.Email,
            NormalizedUserName = Input.Email.ToUpper(),
            Email = Input.Email,
            NormalizedEmail = Input.Email.ToUpper(),
            FirstName = Input.FirstName,
            LastName = Input.LastName
        };


        var result = await _userManager.CreateAsync(user, Input.Password);


        if (result.Succeeded)
        {
            // ⭐ Student čeká na schválení
            var student = new Student
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Email = Input.Email,
                UserId = user.Id,
                IsActive = false,
                CreatedAt = DateTime.Now
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Account/RegisterSuccess");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return Page();
    }
}
