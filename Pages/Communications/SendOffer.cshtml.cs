using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Services;

namespace Vyuka.Pages.Communications
{
    public class SendOfferModel : PageModel
    {
        private readonly OfferEmailBuilder _builder;
        private readonly IEmailService _email;
        private readonly IWebHostEnvironment _env;

        public SendOfferModel(OfferEmailBuilder builder, IEmailService email, IWebHostEnvironment env)
        {
            _builder = builder;
            _email = email;
            _env = env;
        }

        [BindProperty] public string ParentEmail { get; set; } = "";
        [BindProperty] public string ParentName { get; set; } = "";
        [BindProperty] public string StudentName { get; set; } = "";
        [BindProperty] public string CustomText { get; set; } = "";

        public string? PreviewHtml { get; set; }

        public void OnGet() { }

        // ⭐ Náhled – zobrazí QR obrázky z wwwroot
        public IActionResult OnPostPreview()
        {
            var model = new Dictionary<string, string>
            {
                ["ParentName"] = ParentName,
                ["StudentName"] = StudentName,
                ["CustomText"] = CustomText,

                // ⭐ QR obrázky pro náhled
                ["QR1"] = "/images/QR/1_hod_400.jpg",
                ["QR2"] = "/images/QR/10_hod_3500.jpg"
            };

            PreviewHtml = _builder.BuildOffer(model);
            return Page();
        }

        // ⭐ Odeslání – použije CID obrázky
        public async Task<IActionResult> OnPostSend()
        {
            if (string.IsNullOrWhiteSpace(ParentEmail))
            {
                throw new Exception("ParentEmail je prázdný nebo null – formulář ho neposílá.");
            }

            var model = new Dictionary<string, string>
            {
                ["ParentName"] = ParentName,
                ["StudentName"] = StudentName,
                ["CustomText"] = CustomText,
                ["QR1"] = "cid:qr1",
                ["QR2"] = "cid:qr2"
            };

            var html = _builder.BuildOffer(model);

            var attachments = new List<EmailAttachment>
    {
        new EmailAttachment("qr1", Path.Combine(_env.WebRootPath, "images/QR/1_hod_400.jpg")),
        new EmailAttachment("qr2", Path.Combine(_env.WebRootPath, "images/QR/10_hod_3500.jpg"))
    };

            await _email.SendAsync(ParentEmail, "Nabídka online výuky", html, attachments);

            TempData["Message"] = "Nabídka byla úspěšně odeslána.";
            return RedirectToPage("/Communications/Index");
        }

    }
}
