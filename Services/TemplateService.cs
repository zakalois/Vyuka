using System.Text;

namespace Vyuka.Services
{
    public interface ITemplateService
    {
        string RenderTemplate(string templateName, Dictionary<string, string> values);
    }

    public class TemplateService : ITemplateService
    {
        private readonly IWebHostEnvironment _env;

        public TemplateService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string RenderTemplate(string templateName, Dictionary<string, string> values)
        {
            // 🔥 Vždy přidáme .html
            string fileName = templateName + ".html";

            // 🔥 Správná cesta ke složce EmailsTemplates
            var path = Path.Combine(_env.ContentRootPath, "EmailsTemplates", fileName);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Template not found: {path}");

            var html = File.ReadAllText(path, Encoding.UTF8);

            foreach (var kv in values)
            {
                html = html.Replace($"{{{{{kv.Key}}}}}", kv.Value);
            }

            return html;
        }
    }
}