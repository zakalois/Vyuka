using System.Text;
using Vyuka.Models;

namespace Vyuka.Services
{
    public interface ITemplateService
    {
        string RenderTemplate(string templateName, Dictionary<string, string> values);
    }

    public class TemplateService : ITemplateService
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;

        public TemplateService(IWebHostEnvironment env, AppDbContext context)
        {
            _env = env;
            _context = context;
        }

        public string RenderTemplate(string templateName, Dictionary<string, string> values)
        {
            // 1️⃣ Pokus o načtení z databáze
            var dbTemplate = _context.EmailTemplates
                .FirstOrDefault(t => t.Name == templateName);

            string html;

            if (dbTemplate != null)
            {
                html = dbTemplate.Body;
            }
            else
            {
                // 2️⃣ Fallback – načtení HTML souboru
                string fileName = templateName + ".html";
                var path = Path.Combine(_env.ContentRootPath, "EmailsTemplates", fileName);

                if (!File.Exists(path))
                    throw new FileNotFoundException($"Template not found: {path}");

                html = File.ReadAllText(path, Encoding.UTF8);
            }

            // 3️⃣ Nahrazení placeholderů
            foreach (var kv in values)
            {
                html = html.Replace($"{{{{{kv.Key}}}}}", kv.Value);
            }

            return html;
        }
    }
}
