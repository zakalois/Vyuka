using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vyuka.Pages
{
    public class HomeIndexModel : PageModel
    {
        private readonly ILogger<HomeIndexModel> _logger;

        public HomeIndexModel(ILogger<HomeIndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}