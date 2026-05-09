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
            Teachers = await _context.Teachers
     .Include(t => t.User)
     .Select(t => new TeacherListViewModel
     {
         Id = t.Id,
         FirstName = t.User.FirstName,
         LastName = t.User.LastName,
         FullName = t.User.FirstName + " " + t.User.LastName,
         Email = t.User.Email,
         Phone = t.User.PhoneNumber,
         Subjects = "",
         StudentsCount = 0,
         HoursThisMonth = 0,
         IsActive = t.IsActive
     })
     .ToListAsync();

        }
    }
}
