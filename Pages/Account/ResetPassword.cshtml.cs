using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using System.Security.Cryptography;
using System.Text;

public class ResetPasswordModel : PageModel
{
    private readonly AppDbContext _context;

    public ResetPasswordModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty] public string NewPassword { get; set; }
    public string Message { get; set; }

    public IActionResult OnGet(string token)
    {
        var record = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == token);

        if (record == null || record.ExpiresAt < DateTime.Now)
        {
            Message = "Token je neplatný nebo vypršel.";
            return Page();
        }

        TempData["Token"] = token;
        return Page();
    }

    public IActionResult OnPost()
    {
        var token = TempData["Token"]?.ToString();
        if (token == null)
            return RedirectToPage("/Login");

        var record = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == token);
        if (record == null)
            return RedirectToPage("/Login");

        var user = _context.Users.First(u => u.Id == record.UserId);
        user.PasswordHash = Hash(NewPassword);

        _context.PasswordResetTokens.Remove(record);
        _context.SaveChanges();

        Message = "Heslo bylo úspěšně změněno.";
        return Page();
    }

    private string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}
