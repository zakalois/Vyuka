using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Admin.Teachers
{
    public class TeachersModel : PageModel
    {
        private readonly AppDbContext _context;

        public TeachersModel(AppDbContext context)

        {
            _context = context;
        }

        public List<TeacherListViewModel> Teachers { get; set; }

        public async Task OnGetAsync()
        {
            Teachers = await _context.Users
    .Where(u => u.Role == "Teacher")
    .Select(u => new TeacherListViewModel
    {
        Id = u.Id,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email,
        Phone = u.PhoneNumber,
        Subjects = "",
        StudentsCount = 0,
        HoursThisMonth = 0
    })
    .ToListAsync();


        }
    }
}
