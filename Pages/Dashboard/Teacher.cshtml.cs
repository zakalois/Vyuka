using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vyuka.Pages.Dashboard
{
    [Authorize(Roles = "Teacher")]
    public class TeacherModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
