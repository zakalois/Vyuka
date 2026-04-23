using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Vyuka.Models;
using Vyuka.Services;
using System.Security.Cryptography;
using System.Text;

public class ForgotPasswordModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IEmailService _email;

    public ForgotPasswordModel(AppDbContext context, IEmailService email)
    {
        _context = context;
        _email = email;
    }

    [BindProperty] public string Email { get; set; }
    public List<SelectListItem> Users { get; set; }
    public string Message { get; set; }

    public void OnGet()
    {
        Users = _context.Users
            .Select(u => new SelectListItem { Value = u.Email, Text = u.Name })
            .ToList();
    }

    public async Task<IActionResult> OnPost()

    {
        var user = _context.Users.FirstOrDefault(u => u.Email == Email);
        if (user == null)
        {
            Message = "Uživatel nenalezen.";
            return Page();
        }

        var newPassword = GeneratePassword();
        user.PasswordHash = Hash(newPassword);
        _context.SaveChanges();

        await _email.SendAsync(user.Email, "Nové heslo", $"Vaše nové heslo je: {newPassword}");

        Message = "Nové heslo bylo odesláno na e‑mail.";
        return Page();
    }

    private string GeneratePassword()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    private string Hash(string input)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}
