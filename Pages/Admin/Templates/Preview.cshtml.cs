using Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

public class PreviewModel : PageModel
{
    private readonly AppDbContext _context;

    public PreviewModel(AppDbContext context)
    {
        _context = context;
    }

    public EmailTemplate Template { get; set; }

    public IActionResult OnGet(int id)
    {
        Template = _context.EmailTemplates.FirstOrDefault(t => t.Id == id);
        if (Template == null)
            return RedirectToPage("Index");

        return Page();
    }
}
