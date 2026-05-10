using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Students
{
    [Authorize(Roles = "Admin")]
    public class AssignTeacherModel : PageModel
    {
        private readonly AppDbContext _context;

        public AssignTeacherModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int StudentId { get; set; }

        [BindProperty]
        public string SelectedTeacherId { get; set; }

        public string StudentName { get; set; }

        public List<SelectListItem> Teachers { get; set; }

        public IActionResult OnGet(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.Id == id);
            if (student == null)
                return NotFound();

            StudentId = id;
            StudentName = $"{student.LastName} {student.FirstName}";

            Teachers = _context.Users
    .Where(u => u.Role == "Teacher")
    .OrderBy(u => u.LastName)
    .ThenBy(u => u.FirstName)
    .Select(u => new SelectListItem
    {
        Value = u.Id,
        Text = $"{u.LastName} {u.FirstName} ({u.Email})"
    })
    .ToList();


            return Page();
        }

        public IActionResult OnPost()
        {
            var student = _context.Students.FirstOrDefault(s => s.Id == StudentId);
            if (student == null)
                return NotFound();

            student.TeacherId = SelectedTeacherId;
            _context.SaveChanges();

            return RedirectToPage("Index");
        }
    }
}
