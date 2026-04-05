using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Subjects
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Subject Subject { get; set; } = default!;
        public List<SubjectTopic> Topics { get; set; } = new();

        [BindProperty]
        public string NewTopic { get; set; } = string.Empty;

        [BindProperty]
        public string ImportText { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == id);

            if (Subject == null)
                return NotFound();

            Topics = await _context.SubjectTopics
                .Where(t => t.SubjectId == id)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddTopicAsync(int id)
        {
            if (string.IsNullOrWhiteSpace(NewTopic))
                return RedirectToPage(new { id });

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound();

            // Kontrola duplicity
            bool exists = await _context.SubjectTopics
                .AnyAsync(t => t.SubjectId == id && t.Name == NewTopic);

            if (!exists)
            {
                var topic = new SubjectTopic
                {
                    Name = NewTopic,
                    SubjectId = id
                };

                _context.SubjectTopics.Add(topic);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostImportAsync(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(ImportText))
            {
                var lines = ImportText
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0)
                    .Distinct()
                    .ToList();

                foreach (var line in lines)
                {
                    bool exists = await _context.SubjectTopics
                        .AnyAsync(t => t.SubjectId == id && t.Name == line);

                    if (!exists)
                    {
                        _context.SubjectTopics.Add(new SubjectTopic
                        {
                            Name = line,
                            SubjectId = id
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }
    }
}