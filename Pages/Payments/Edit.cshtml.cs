using Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Payments
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Payment Payment { get; set; }

        public SelectList StudentList { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Payment = await _context.Payments.FindAsync(id);

            if (Payment == null)
                return NotFound();

            StudentList = new SelectList(_context.Students, "Id", "FullName");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                StudentList = new SelectList(_context.Students, "Id", "FullName");
                return Page();
            }

            _context.Attach(Payment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToPage("/Payments/Index");
        }
    }
}
