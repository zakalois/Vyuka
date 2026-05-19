using Google;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<EmailTemplate> Templates { get; set; }

    public void OnGet()
    {
        Templates = _context.EmailTemplates
            .OrderBy(t => t.Name)
            .ToList();
    }
}
