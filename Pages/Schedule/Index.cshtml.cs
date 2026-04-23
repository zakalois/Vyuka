using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Schedule
{
    public class ScheduleIndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _email;
        private readonly LessonPlanEmailBuilder _emailBuilder;
        private readonly GoogleCalendarService _calendar;

        public ScheduleIndexModel(
            AppDbContext context,
            GoogleCalendarService calendar,
            IEmailService email,
            LessonPlanEmailBuilder emailBuilder
        )
        {
            _context = context;
            _calendar = calendar;   // ← správně
            _email = email;
            _emailBuilder = emailBuilder;
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

        private void ComputeWeek()
        {
            if (Week == default)
                Week = DateTime.Today;

            int diff = (7 + (Week.DayOfWeek - DayOfWeek.Monday)) % 7;
            StartOfWeek = Week.AddDays(-diff).Date;
            EndOfWeek = StartOfWeek.AddDays(6);
        }

        private async Task LoadDropdownsAsync()
        {
            Students = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.LastName)
                .ToListAsync();

            Subjects = await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();

            Topics = await _context.SubjectTopics
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

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

        public async Task<JsonResult> OnGetTopicsAsync(int subjectId)
        {
            var topics = await _context.SubjectTopics
                .Where(t => t.SubjectId == subjectId)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            return new JsonResult(topics);
        }

        // 🔵 PŘIDÁNÍ PLÁNOVANÉ HODINY + GENEROVÁNÍ MEET ODKAZU
        public async Task<IActionResult> OnPostAddAsync()
        {
            ComputeWeek();

            if (NewSubjectId <= 0)
            {
                ModelState.AddModelError("NewSubjectId", "Musíte vybrat předmět.");
                await LoadDropdownsAsync();
                return Page();
            }

            if (NewStart == default)
            {
                ModelState.AddModelError("", "Musíte zadat začátek hodiny.");
                await LoadDropdownsAsync();
                return Page();
            }

            var student = await _context.Students.FindAsync(NewStudentId);
            if (student == null || !student.IsActive)
            {
                ModelState.AddModelError("", "Tomuto studentovi nelze přiřadit hodinu, protože je neaktivní.");
                await LoadDropdownsAsync();
                return Page();
            }

            if (NewEnd == default || NewEnd <= NewStart)
            {
                NewEnd = NewStart + TimeSpan.FromHours(1);
            }

            int dayIndex = NewDay == DayOfWeek.Sunday ? 6 : ((int)NewDay - 1);
            var date = StartOfWeek.AddDays(dayIndex);

            // 🔵 Načtení předmětu
            var subject = await _context.Subjects.FindAsync(NewSubjectId);

            // 🔵 Načtení tématu (může být null)
            var topic = NewTopicId.HasValue
                ? await _context.SubjectTopics.FindAsync(NewTopicId.Value)
                : null;

            // 🔵 Název události pro Google Calendar
            var title = $"{subject?.Name} – {topic?.Name}";

            // 🔵 Spojení data + času na DateTime
            var startDateTime = date.Date + NewStart;
            var endDateTime = date.Date + NewEnd;

            // 🔵 Vytvoření Meet odkazu
            var meetLink = await _calendar.CreateMeetEventAsync(
                startDateTime,
                endDateTime,
                title,
                student.Email,
                "zakalois@ucitelzak.eu",
                student.FirstName,
                student.LastName
            );

            // 🔵 Vygenerování HTML e‑mailu
            var html = await _emailBuilder.BuildAsync(
                $"{student.FirstName} {student.LastName}",
                subject?.Name ?? "",
                topic?.Name ?? "",
                date,
                NewStart,      // ← správně: TimeSpan
                meetLink
            );

            // 🔵 Odeslání e‑mailu studentovi
            await _email.SendAsync(
                  student.Email,
                  "Plánovaná lekce",
                  html
            );

            // 🔵 Uložení plánované hodiny do DB
            var plan = new LessonPlan
            {
                StudentId = NewStudentId,
                SubjectId = NewSubjectId,
                SubjectTopicId = NewTopicId,
                Day = NewDay,
                Start = NewStart,
                End = NewEnd,
                Date = date,
                MeetLink = meetLink
            };

            _context.LessonPlans.Add(plan);
            await _context.SaveChangesAsync();

            // 🔵 Návrat zpět na rozvrh
            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        // 🔵 ODUČENÍ – BEZ GENEROVÁNÍ MEET
        public async Task<IActionResult> OnPostTeachAsync(int id)
        {
            ComputeWeek();

            var plan = await _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
                return NotFound();

            int hours = (int)(plan.End - plan.Start).TotalHours;

            var lesson = new Lesson
            {
                StudentId = plan.StudentId,
                SubjectId = plan.SubjectId,
                SubjectTopicId = plan.SubjectTopicId,
                Date = plan.Date,
                Hours = hours,
                IsTaught = true
            };

            _context.Lessons.Add(lesson);
            plan.IsTaught = true;

            await _context.SaveChangesAsync();

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            ComputeWeek();

            var plan = await _context.LessonPlans.FindAsync(id);

            if (plan != null)
            {
                var hours = (int)(plan.End - plan.Start).TotalHours;

                var lesson = await _context.Lessons
                    .FirstOrDefaultAsync(l =>
                        l.StudentId == plan.StudentId &&
                        l.SubjectId == plan.SubjectId &&
                        l.Date.Date == plan.Date.Date &&
                        l.Hours == hours &&
                        l.IsTaught == true);

                if (lesson != null)
                {
                    _context.Lessons.Remove(lesson);
                }

                _context.LessonPlans.Remove(plan);

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

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