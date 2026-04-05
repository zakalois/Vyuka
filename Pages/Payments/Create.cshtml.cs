using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Payments
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        public SelectList StudentList { get; set; } = default!;

        [BindProperty]
        public int SelectedStudentId { get; set; }

        [BindProperty]
        public DateTime Date { get; set; } = DateTime.Today;

        [BindProperty]
        public decimal Amount { get; set; }

        [BindProperty]
        public decimal HoursPurchased { get; set; }

        [BindProperty]
        public decimal? PricePerHour { get; set; }

        [BindProperty]
        public string? Method { get; set; }

        [BindProperty]
        public string? Note { get; set; }

        public async Task OnGetAsync()
        {
            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            StudentList = new SelectList(
                students.Select(s => new
                {
                    Id = s.Id,
                    FullName = $"{s.LastName} {s.FirstName}"
                }),
                "Id",
                "FullName"
            );
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // znovu načteme dropdown
                return Page();
            }

            var payment = new Payment
            {
                StudentId = SelectedStudentId,
                Date = Date,
                Amount = Amount,
                HoursPurchased = HoursPurchased,
                PricePerHour = PricePerHour,
                Method = Method,
                Note = Note
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Payments/Create");
        }
    }
}