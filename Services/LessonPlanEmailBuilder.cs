using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Vyuka.Services
{
    public class LessonPlanEmailBuilder
    {
        private readonly IWebHostEnvironment _env;

        public LessonPlanEmailBuilder(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> BuildAsync(
            string studentName,
            string subjectName,
            string lessonTopic,
            DateTime date,
            TimeSpan start,
            string meetLink)
        {
            // Absolutní cesta k šabloně
            var path = Path.Combine(_env.ContentRootPath, "EmailsTemplates", "LessonPlanEmail.html");

            // Načtení HTML
            var html = await File.ReadAllTextAsync(path);

            // Nahrazení placeholderů
            html = html.Replace("{{StudentName}}", studentName)
                       .Replace("{{SubjectName}}", subjectName)
                       .Replace("{{LessonTopic}}", lessonTopic)
                       .Replace("{{LessonDate}}", date.ToString("dd.MM.yyyy"))
                       .Replace("{{LessonTime}}", start.ToString(@"hh\:mm"))
                       .Replace("{{MeetLink}}", meetLink);

            return html;
        }
    }
}