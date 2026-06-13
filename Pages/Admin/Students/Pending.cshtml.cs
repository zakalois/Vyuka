using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

public class PendingModel : PageModel
{
    private readonly AppDbContext _context;

    public PendingModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Student> PendingStudents { get; set; }

    public async Task OnGetAsync()
    {
        PendingStudents = await _context.Students
            .Where(s => !s.IsActive)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }
}
