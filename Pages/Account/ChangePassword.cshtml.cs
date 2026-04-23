using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using System.Security.Cryptography;
using System.Text;

public class ChangePasswordModel : PageModel
{
    private readonly AppDbContext _context;

    public ChangePasswordModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty] public string CurrentPassword { get; set; }
    [BindProperty] public string NewPassword { get; set; }
    public string Message { get; set; }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
            return RedirectToPage("/Login");

        return Page();
    }

    public IActionResult OnPost()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToPage("/Login");

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return RedirectToPage("/Login");

        if (user.PasswordHash != Hash(CurrentPassword))
        {
            Message = "Současné heslo není správné.";
            return Page();
        }

        user.PasswordHash = Hash(NewPassword);
        _context.SaveChanges();

        Message = "Heslo bylo úspěšně změněno.";
        return Page();
    }

    private string Hash(string input)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}
