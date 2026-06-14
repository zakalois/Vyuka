using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vyuka.Models;
using Vyuka.Services;

namespace Vyuka.Pages.Admin.Schedule
{
    [Authorize(Roles = "Admin,Teacher")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly GoogleCalendarService _calendar;
        private readonly IEmailService _email;
        private readonly LessonEmailBuilder _emailBuilder;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(
            AppDbContext context,
            GoogleCalendarService calendar,
            IEmailService email,
            LessonEmailBuilder emailBuilder,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _calendar = calendar;
            _email = email;
            _emailBuilder = emailBuilder;
            _userManager = userManager;
        }

        // -----------------------------
        // BARVY STUDENTŮ
        // -----------------------------
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
            if (studentId <= 0)
                return "#cccccc";

            return StudentColors[studentId % StudentColors.Length];
        }

        // -----------------------------
        // DATA PRO STRÁNKU
        // -----------------------------
        public IList<LessonPlan> Plans { get; set; } = new List<LessonPlan>();
        public List<Student> Students { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<SubjectTopic> Topics { get; set; } = new();
        public List<AppUser> AllTeachers { get; set; } = new();

        // FILTRY
        [BindProperty(SupportsGet = true)]
        public DateTime Week { get; set; }

        // TeacherId z URL → string, ale vždy převádíme na int
        [BindProperty(SupportsGet = true)]
        public string? TeacherId { get; set; }

        // VÝPOČET TÝDNE
        [BindProperty] public DateTime StartOfWeek { get; set; }
        [BindProperty] public DateTime EndOfWeek { get; set; }

        // PŘIDÁVÁNÍ LEKCÍ
        [BindProperty] public int? NewStudentId { get; set; }
        [BindProperty] public int? NewSubjectId { get; set; }
        [BindProperty] public int? NewTopicId { get; set; }
        [BindProperty] public TimeSpan? NewStart { get; set; }
        [BindProperty] public TimeSpan? NewEnd { get; set; }
        [BindProperty] public bool NotifyStudentOnDelete { get; set; }
        [BindProperty] public DateTime? NewDate { get; set; }

        [BindProperty]
        public LessonPlan EditPlan { get; set; }

        // -----------------------------
        // METODY
        // -----------------------------
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

        // ⭐ Handler pro AJAX načítání témat
        public JsonResult OnGetTopics(int subjectId)
        {
            var topics = _context.SubjectTopics
                .Where(t => t.SubjectId == subjectId)
                .Select(t => new { id = t.Id, name = t.Name })
                .ToList();

            return new JsonResult(topics);
        }
        // -----------------------------
        // GET – NAČTENÍ STRÁNKY
        // -----------------------------
        public async Task OnGetAsync()
        {
            ComputeWeek();
            await LoadDropdownsAsync();

            // Učitelé
            AllTeachers = (await _userManager.GetUsersInRoleAsync("Teacher"))
                .OrderBy(t => t.LastName)
                .ThenBy(t => t.FirstName)
                .ToList();

            // Získání ID aktuálně přihlášeného uživatele
            var currentUserId = User.FindFirst("sub")?.Value;

            // Získání Teacher.Id (int)
            int? currentTeacherId = _context.Teachers
                .Where(t => t.UserId == currentUserId)
                .Select(t => t.Id)
                .SingleOrDefault();

            // Filtrování rozvrhu
            var query = _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .Include(p => p.Teacher)
                .Where(p => p.Date >= StartOfWeek && p.Date <= EndOfWeek);
            var taughtLessons = await _context.Lessons
                .Include(l => l.Student)
                .Include(l => l.Subject)
                .Include(l => l.SubjectTopic)
                .Include(l => l.Teacher)
                .Where(l => l.Date >= StartOfWeek && l.Date <= EndOfWeek)
                .ToListAsync();

            ViewData["TaughtLessons"] = taughtLessons;

            // UČITEL → vidí jen svůj rozvrh
            if (User.IsInRole("Teacher"))
            {
                query = query.Where(p => p.TeacherId == currentTeacherId);
            }
            else
            {
                // ADMIN → filtr podle TeacherId z URL
                if (int.TryParse(TeacherId, out var tid))
                    query = query.Where(p => p.TeacherId == tid);
            }

            Plans = await query
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Start)
                .ToListAsync();
        }

        // -----------------------------
        // PŘIDÁNÍ LEKCE
        // -----------------------------
        public async Task<IActionResult> OnPostAddAsync(DateTime Week)
        {
            this.Week = Week;
            ComputeWeek();
            await LoadDropdownsAsync();

            // VALIDACE
            if (NewStudentId == null) throw new Exception("❌ Student není vybrán");
            if (NewSubjectId == null) throw new Exception("❌ Předmět není vybrán");
            if (NewDate == null) throw new Exception("❌ Datum lekce není vybráno");
            if (NewStart == null) throw new Exception("❌ Začátek lekce není vybrán");
            if (NewEnd == null) throw new Exception("❌ Konec lekce není vybrán");
            if (NewEnd <= NewStart) throw new Exception("❌ Konec musí být později než začátek");

            var student = await _context.Students.FindAsync(NewStudentId.Value);
            var subject = await _context.Subjects.FindAsync(NewSubjectId.Value);
            var topic = NewTopicId.HasValue ? await _context.SubjectTopics.FindAsync(NewTopicId.Value) : null;

            var date = NewDate.Value;

            // GOOGLE MEET
            var meet = await _calendar.CreateMeetEventAsync(
                date + NewStart.Value,
                date + NewEnd.Value,
                $"{subject.Name} – {topic?.Name}",
                student.Email,
                "zakalois@ucitelzak.eu",
                student.FirstName,
                student.LastName,
                "#90CAF9"
            );

            // EMAIL
            var html = await _emailBuilder.BuildPlannedAsync(
                $"{student.FirstName} {student.LastName}",
                subject.Name,
                topic?.Name ?? "",
                date,
                NewStart.Value,
                meet.MeetLink
            );

            await _email.SendAsync(
    student.Email,
    "Plánovaná lekce",
    html,
    null,                                   // attachments
    null,                                   // dynamicAmount
    null,                                   // dynamicMessage
    null,                                   // customText
    $"{student.FirstName} {student.LastName}", // studentName
    "lesson_planned",                       // emailType
    student.Id                              // studentId
);


            // URČENÍ TEACHER ID – vždy podle studenta
            int? teacherIdToAssign = _context.Students
                .Where(s => s.Id == NewStudentId.Value)
                .Select(s => s.TeacherId)
                .SingleOrDefault();


            // ULOŽENÍ LEKCE
            var plan = new LessonPlan
            {
                StudentId = NewStudentId.Value,
                SubjectId = NewSubjectId.Value,
                SubjectTopicId = NewTopicId,
                Start = NewStart.Value,
                End = NewEnd.Value,
                Date = date,
                MeetLink = meet.MeetLink,
                GoogleEventId = meet.EventId,
                NotifyOnDelete = NotifyStudentOnDelete,
                TeacherId = teacherIdToAssign
            };

            _context.LessonPlans.Add(plan);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd"), TeacherId });
        }

        // -----------------------------
        // EDITACE – ZAČÁTEK
        // -----------------------------
        public async Task<IActionResult> OnPostEditStartAsync(int id, DateTime Week, string? TeacherId)
        {
            this.Week = Week;
            ComputeWeek();
            await LoadDropdownsAsync();

            this.TeacherId = TeacherId;

            var query = _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .AsQueryable();

            if (int.TryParse(TeacherId, out var tid))
                query = query.Where(p => p.TeacherId == tid);

            EditPlan = await query.FirstOrDefaultAsync(p => p.Id == id);

            if (EditPlan == null)
            {
                return RedirectToPage(new
                {
                    week = Week.ToString("yyyy-MM-dd"),
                    TeacherId = this.TeacherId
                });
            }

            var plansQuery = _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .Where(p => p.Date >= StartOfWeek && p.Date <= EndOfWeek);

            if (int.TryParse(TeacherId, out var tid2))
                plansQuery = plansQuery.Where(p => p.TeacherId == tid2);

            Plans = await plansQuery
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Start)
                .ToListAsync();

            return Page();
        }

        // -----------------------------
        // UPDATE LEKCE
        // -----------------------------
        public async Task<IActionResult> OnPostUpdateAsync(
            int id,
            int StudentId,
            int SubjectId,
            int? SubjectTopicId,
            TimeSpan Start,
            TimeSpan End,
            DateTime Date,
            DateTime Week)
        {
            this.Week = Week;
            ComputeWeek();

            var plan = await _context.LessonPlans.FindAsync(id);
            if (plan == null)
                return NotFound();

            plan.StudentId = StudentId;
            plan.SubjectId = SubjectId;
            plan.SubjectTopicId = SubjectTopicId;
            plan.Start = Start;
            plan.End = End;
            plan.Date = Date;

            await _context.SaveChangesAsync();

            var newWeekStart = Date.AddDays(-(int)Date.DayOfWeek + 1);

            return RedirectToPage(new { week = newWeekStart.ToString("yyyy-MM-dd"), TeacherId });
        }

        // -----------------------------
        // ZRUŠENÍ EDITACE
        // -----------------------------
        public IActionResult OnPostEditCancel(DateTime Week)
        {
            this.Week = Week;
            ComputeWeek();
            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd"), TeacherId });
        }

        // -----------------------------
        // SMAZÁNÍ LEKCE
        // -----------------------------
        public async Task<IActionResult> OnPostDeleteAsync(int id, DateTime Week)
        {
            this.Week = Week;
            ComputeWeek();

            var plan = await _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan != null)
            {
                if (!string.IsNullOrEmpty(plan.GoogleEventId))
                    await _calendar.DeleteEventAsync(plan.GoogleEventId);

                var html = await _emailBuilder.BuildCanceledAsync(
                    $"{plan.Student.FirstName} {plan.Student.LastName}",
                    plan.Subject?.Name ?? "",
                    plan.SubjectTopic?.Name ?? "",
                    plan.Date,
                    plan.Start,
                    plan.End
                );

                await _email.SendAsync(plan.Student.Email, "Zrušená lekce", html);

                var lesson = await _context.Lessons
                    .FirstOrDefaultAsync(l =>
                        l.StudentId == plan.StudentId &&
                        l.SubjectId == plan.SubjectId &&
                        l.Date.Date == plan.Date.Date &&
                        l.Start == plan.Start &&
                        l.End == plan.End &&
                        l.IsTaught == true);

                if (lesson != null)
                    _context.Lessons.Remove(lesson);

                _context.LessonPlans.Remove(plan);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd"), TeacherId });
        }

        // -----------------------------
        // OZNAČENÍ LEKCE JAKO ODUČENÉ
        // -----------------------------
        public async Task<IActionResult> OnPostTeachAsync(int id, DateTime Week)
        {
            this.Week = Week;
            ComputeWeek();

            var plan = await _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
                return NotFound();

            var hours = (decimal)(plan.End - plan.Start).TotalHours;

            _context.Lessons.Add(new Lesson
            {
                StudentId = plan.StudentId,
                SubjectId = plan.SubjectId,
                SubjectTopicId = plan.SubjectTopicId,
                Date = plan.Date,
                Day = (int)plan.Date.DayOfWeek,
                Start = plan.Start,
                End = plan.End,
                Hours = hours,
                IsTaught = true,
                MeetLink = plan.MeetLink,
                TeacherId = plan.TeacherId   // ⭐ DOPLNĚNO
            });


            plan.IsTaught = true;
            await _context.SaveChangesAsync();

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd"), TeacherId });
        }
    }
}
