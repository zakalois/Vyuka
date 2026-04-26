using Microsoft.AspNetCore.Mvc.RazorPages;
using Vyuka.Models;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace Vyuka.Pages.Dashboard
{
    public class AdminModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminModel(AppDbContext context)
        {
            _context = context;
        }

        public DateTime? NextLessonDate { get; set; }

        public void OnGet()
        {
            var lessons = _context.LessonPlans
                .Where(x => x.Date >= DateTime.Today)
                .ToList();

            NextLessonDate = lessons
                .Select(x => new DateTime(
                    x.Date.Year,
                    x.Date.Month,
                    x.Date.Day,
                    x.Start.Hours,
                    x.Start.Minutes,
                    x.Start.Seconds
                ))
                .Where(dt => dt > DateTime.Now)
                .OrderBy(dt => dt)
                .FirstOrDefault();
        }

        // 🔥 SEM PATŘÍ HANDLER PRO ZÁLOHU 🔥
        public async Task<IActionResult> OnPostBackupAsync()
        {
            Console.WriteLine("BACKUP HANDLER SE SPUSTIL");
            TempData["Message"] = "Handler se spustil.";

            try
            {
                string backupFolder = @"D:\VS_Programy\Zaloha_Vyuka";
                Directory.CreateDirectory(backupFolder);

                string fileName = $"VyukaDb_{DateTime.Now:yyyy-MM-dd_HH-mm}.bak";
                string fullPath = Path.Combine(backupFolder, fileName);

                string sqlPath = fullPath.Replace("\\", "\\\\");

                string sql = "BACKUP DATABASE [VyukaDb] TO DISK = @path WITH INIT, STATS = 10;";

                using (var connection = new SqlConnection("Server=localhost;Database=master;Trusted_Connection=True;"))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@path", sqlPath);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["Message"] = $"Záloha byla vytvořena: {fileName}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Chyba při zálohování: " + ex.ToString();
            }

            return RedirectToPage();
        }



    }
}