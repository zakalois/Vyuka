using Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)

    {
        _context = context;
    }

    [BindProperty]
    public EmailTemplate Template { get; set; }

    public IActionResult OnGet(int id)
    {
        Template = _context.EmailTemplates.FirstOrDefault(t => t.Id == id);

        if (Template == null)
            return RedirectToPage("Index");

        return Page();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        _context.EmailTemplates.Update(Template);
        _context.SaveChanges();

        return RedirectToPage("Index");
    }
}
