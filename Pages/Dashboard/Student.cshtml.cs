using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vyuka.Pages.Dashboard
{
    [Authorize(Roles = "Student")]
    public class StudentModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
