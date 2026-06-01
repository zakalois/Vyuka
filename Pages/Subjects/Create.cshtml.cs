using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;

namespace Vyuka.Pages.Subjects
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public Subject Subject { get; set; } = new Subject();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (ImageFile != null)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(_env.WebRootPath, "images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                Subject.ImageUrl = fileName;
            }

            if (string.IsNullOrWhiteSpace(Subject.Topics))
                Subject.Topics = "";

            // ⭐ DŮLEŽITÉ – bez toho se předmět NEULOŽÍ
            Subject.TeacherId = 1;

            _context.Subjects.Add(Subject);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

    }
}
