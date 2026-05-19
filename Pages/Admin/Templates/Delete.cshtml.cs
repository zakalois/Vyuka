using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Templates
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context)
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

        public IActionResult OnPost(int id)
        {
            var template = _context.EmailTemplates.FirstOrDefault(t => t.Id == id);

            if (template != null)
            {
                _context.EmailTemplates.Remove(template);
                _context.SaveChanges();
            }

            return RedirectToPage("Index");
        }
    }
}
