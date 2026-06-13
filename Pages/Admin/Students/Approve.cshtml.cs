using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

public class ApproveModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ApproveModel(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Student Student { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Student = await _context.Students.FindAsync(id);

        if (Student == null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var student = await _context.Students.FindAsync(id);

        if (student == null)
            return NotFound();

        // ⭐ 1) Aktivace studenta
        student.IsActive = true;

        // ⭐ 2) Najdeme jeho uživatelský účet
        var user = await _userManager.FindByIdAsync(student.UserId);

        if (user != null)
        {
            // ⭐ 3) Přidáme roli Student
            await _userManager.AddToRoleAsync(user, "Student");
        }

        // ⭐ 4) Uložíme změny
        await _context.SaveChangesAsync();

        // ⭐ 5) Návrat na Pending
        return RedirectToPage("Pending");
    }
}
