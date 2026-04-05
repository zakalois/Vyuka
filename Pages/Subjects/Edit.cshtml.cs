using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Subjects
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EditModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public Subject Subject { get; set; } = default!;

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound();

            Subject = subject;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var subjectInDb = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == Subject.Id);
            if (subjectInDb == null)
                return NotFound();

            subjectInDb.Name = Subject.Name;
            subjectInDb.Topics = Subject.Topics;

            if (ImageFile != null)
            {
                var fileName = Path.GetFileName(ImageFile.FileName);
                var filePath = Path.Combine(_env.WebRootPath, "images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                subjectInDb.ImageUrl = fileName;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}