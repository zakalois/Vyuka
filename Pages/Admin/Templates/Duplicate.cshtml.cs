using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Templates
{
    public class DuplicateModel : PageModel
    {
        private readonly AppDbContext _context;

        public DuplicateModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int id)
        {
            var original = _context.EmailTemplates.FirstOrDefault(t => t.Id == id);
            if (original == null)
                return RedirectToPage("Index");

            var copy = new EmailTemplate
            {
                Name = original.Name + " (kopie)",
                Subject = original.Subject,
                Body = original.Body
            };

            _context.EmailTemplates.Add(copy);
            _context.SaveChanges();

            return RedirectToPage("Edit", new { id = copy.Id });
        }
    }
}
