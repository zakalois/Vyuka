using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

public class ProfileModel : PageModel
{
    private readonly AppDbContext _context;

    public ProfileModel(AppDbContext context)
    {
        _context = context;
    }

    public Vyuka.Models.AppUser User { get; set; }


    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
            return RedirectToPage("/Login");

        User = _context.Users.FirstOrDefault(u => u.Id == userId);

        if (User == null)
            return RedirectToPage("/Login");

        return Page();
    }
}
