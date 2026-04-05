using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Schedule
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        // 25 barev
        private static readonly string[] StudentColors = new[]
        {
            "#FFCDD2","#F8BBD0","#E1BEE7","#D1C4E9","#C5CAE9",
            "#BBDEFB","#B3E5FC","#B2EBF2","#B2DFDB","#C8E6C9",
            "#DCEDC8","#F0F4C3","#FFF9C4","#FFECB3","#FFE0B2",
            "#FFCCBC","#D7CCC8","#CFD8DC","#F5F5F5","#E0F7FA",
            "#E8F5E9","#FFF3E0","#F3E5F5","#EDE7F6","#E1F5FE"
        };

        public string GetColorForStudent(int studentId)
        {
            return StudentColors[studentId % StudentColors.Length];
        }

        public IList<Lesson> Lessons { get; set; }

        // Parametry týdne
        [BindProperty(SupportsGet = true)]
        public DateTime Week { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime StartOfWeek { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime EndOfWeek { get; set; }

        // Data pro formulář
        public List<Student> Students { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<SubjectTopic> Topics { get; set; } = new();
        public List<LessonPlan> Plans { get; set; } = new();

        // BindProperty pro přidání hodiny
        [BindProperty] public int NewStudentId { get; set; }
        [BindProperty] public int NewSubjectId { get; set; }
        [BindProperty] public int? NewTopicId { get; set; }
        [BindProperty] public DayOfWeek NewDay { get; set; }
        [BindProperty] public TimeSpan NewStart { get; set; }
        [BindProperty] public TimeSpan NewEnd { get; set; }

        // Výpočet týdne
        private void ComputeWeek()
        {
            if (Week == default)
                Week = DateTime.Today;

            int diff = (7 + (Week.DayOfWeek - DayOfWeek.Monday)) % 7;
            StartOfWeek = Week.AddDays(-diff).Date;
            EndOfWeek = StartOfWeek.AddDays(6);
        }

        // 🔵 Společná metoda pro dropdowny
        private async Task LoadDropdownsAsync()
        {
            Students = await _context.Students.OrderBy(s => s.LastName).ToListAsync();
            Subjects = await _context.Subjects.OrderBy(s => s.Name).ToListAsync();
            Topics = await _context.SubjectTopics.OrderBy(t => t.Name).ToListAsync();
        }

        // GET
        public async Task OnGetAsync()
        {
            ComputeWeek();
            await LoadDropdownsAsync();

            Plans = await _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .Where(p => p.Date >= StartOfWeek && p.Date <= EndOfWeek)
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Start)
                .ToListAsync();
        }

        // AJAX – načtení témat
        public async Task<JsonResult> OnGetTopicsAsync(int subjectId)
        {
            var topics = await _context.SubjectTopics
                .Where(t => t.SubjectId == subjectId)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            return new JsonResult(topics);
        }

        // ✔ Přidání hodiny do rozvrhu
        public async Task<IActionResult> OnPostAddAsync()
        {
            ComputeWeek();

            // VALIDACE – musí být vybrán předmět
            if (NewSubjectId <= 0)
            {
                ModelState.AddModelError("NewSubjectId", "Musíte vybrat předmět.");
                await LoadDropdownsAsync();
                return Page();
            }

            // VALIDACE – časová pole musí být vyplněna
            if (NewStart == default || NewEnd == default)
            {
                ModelState.AddModelError("", "Musíte zadat začátek i konec hodiny.");
                await LoadDropdownsAsync();
                return Page();
            }

            // VALIDACE – konec musí být později než začátek
            if (NewEnd <= NewStart)
            {
                ModelState.AddModelError("", "Konec hodiny musí být později než začátek.");
                await LoadDropdownsAsync();
                return Page();
            }

            // Výpočet data podle dne v týdnu
            int dayIndex = NewDay == DayOfWeek.Sunday ? 6 : ((int)NewDay - 1);
            var date = StartOfWeek.AddDays(dayIndex);

            var plan = new LessonPlan
            {
                StudentId = NewStudentId,
                SubjectId = NewSubjectId,
                SubjectTopicId = NewTopicId,
                Day = NewDay,
                Start = NewStart,
                End = NewEnd,
                Date = date
            };

            _context.LessonPlans.Add(plan);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        // ✔ Označení hodiny jako odučené
        public async Task<IActionResult> OnPostTeachAsync(int id)
        {
            ComputeWeek();

            var plan = await _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
                return NotFound();

            // VŽDY vytvořit nový Lesson
            int hours = (int)(plan.End - plan.Start).TotalHours;

            var lesson = new Lesson
            {
                StudentId = plan.StudentId,
                SubjectId = plan.SubjectId,
                Date = plan.Date,
                Hours = hours,
                IsTaught = true,
                Type = "Standard"
            };

            _context.Lessons.Add(lesson);

            // LessonPlan jen označíme jako odučený (kvůli UI)
            plan.IsTaught = true;

            await _context.SaveChangesAsync();

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        // ✔ Smazání hodiny + odpovídající odučené lekce
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            ComputeWeek();

            // Najdeme LessonPlan
            var plan = await _context.LessonPlans.FindAsync(id);

            if (plan != null)
            {
                // Spočítáme počet hodin podle Start/End
                var hours = (int)(plan.End - plan.Start).TotalHours;

                // Najdeme odpovídající Lesson
                var lesson = await _context.Lessons
                    .FirstOrDefaultAsync(l =>
                        l.StudentId == plan.StudentId &&
                        l.SubjectId == plan.SubjectId &&
                        l.Date.Date == plan.Date.Date &&
                        l.Hours == hours &&
                        l.IsTaught == true);

                // Pokud existuje, smažeme ji
                if (lesson != null)
                {
                    _context.Lessons.Remove(lesson);
                }

                // Smažeme LessonPlan
                _context.LessonPlans.Remove(plan);

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        // ✔ Editace
        [BindProperty]
        public LessonPlan EditPlan { get; set; }

        public async Task<IActionResult> OnPostEditAsync(int id)
        {
            ComputeWeek();
            await LoadDropdownsAsync();

            EditPlan = await _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .FirstOrDefaultAsync(p => p.Id == id);

            return Page();
        }

        // ✔ Uložení změn
        public async Task<IActionResult> OnPostSaveAsync()
        {
            ComputeWeek();

            var plan = await _context.LessonPlans.FindAsync(EditPlan.Id);

            if (plan != null)
            {
                plan.StudentId = EditPlan.StudentId;
                plan.SubjectId = EditPlan.SubjectId;
                plan.SubjectTopicId = EditPlan.SubjectTopicId;
                plan.Start = EditPlan.Start;
                plan.End = EditPlan.End;
                plan.Day = EditPlan.Day;

                if (EditPlan.Date != default && EditPlan.Date != plan.Date)
                {
                    plan.Date = EditPlan.Date.Date;
                }
                else
                {
                    int dayIndex = plan.Day == DayOfWeek.Sunday ? 6 : ((int)plan.Day - 1);
                    plan.Date = StartOfWeek.AddDays(dayIndex);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }
    }
}