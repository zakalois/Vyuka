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
        private readonly GoogleCalendarService _calendar;
        private readonly IEmailService _email;
        private readonly LessonEmailBuilder _emailBuilder;

        public ScheduleIndexModel(
            AppDbContext context,
            GoogleCalendarService calendar,
            IEmailService email,
            LessonEmailBuilder emailBuilder)
        {
            _context = context;
            _calendar = calendar;
            _email = email;
            _emailBuilder = emailBuilder;
        }

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

        public IList<LessonPlan> Plans { get; set; } = new List<LessonPlan>();
        public List<Student> Students { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<SubjectTopic> Topics { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime Week { get; set; }

        [BindProperty]
        public DateTime StartOfWeek { get; set; }

        [BindProperty]
        public DateTime EndOfWeek { get; set; }

        [BindProperty] public int NewStudentId { get; set; }
        [BindProperty] public int NewSubjectId { get; set; }
        [BindProperty] public int? NewTopicId { get; set; }
        [BindProperty] public DayOfWeek NewDay { get; set; }
        [BindProperty] public TimeSpan NewStart { get; set; }
        [BindProperty] public TimeSpan NewEnd { get; set; }
        [BindProperty] public bool NotifyStudentOnDelete { get; set; }

        [BindProperty]
        public LessonPlan EditPlan { get; set; }

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

        // ADD
        public async Task<IActionResult> OnPostAddAsync(DateTime Week)
        {
            this.Week = Week;
            ComputeWeek();
            await LoadDropdownsAsync();

            var student = await _context.Students.FindAsync(NewStudentId);
            var subject = await _context.Subjects.FindAsync(NewSubjectId);
            var topic = NewTopicId.HasValue
                ? await _context.SubjectTopics.FindAsync(NewTopicId.Value)
                : null;

            int dayIndex = NewDay == DayOfWeek.Sunday ? 6 : ((int)NewDay - 1);
            var date = StartOfWeek.AddDays(dayIndex);

            if (NewEnd <= NewStart)
                NewEnd = NewStart + TimeSpan.FromHours(1);

            var meet = await _calendar.CreateMeetEventAsync(
                date + NewStart,
                date + NewEnd,
                $"{subject?.Name} – {topic?.Name}",
                student.Email,
                "zakalois@ucitelzak.eu",
                student.FirstName,
                student.LastName,
                "#90CAF9"
            );

            var html = await _emailBuilder.BuildPlannedAsync(
                $"{student.FirstName} {student.LastName}",
                subject?.Name ?? "",
                topic?.Name ?? "",
                date,
                NewStart,
                meet.MeetLink
            );

            await _email.SendAsync(student.Email, "Plánovaná lekce", html);

            var plan = new LessonPlan
            {
                StudentId = NewStudentId,
                SubjectId = NewSubjectId,
                SubjectTopicId = NewTopicId,
                Day = NewDay,
                Start = NewStart,
                End = NewEnd,
                Date = date,
                MeetLink = meet.MeetLink,
                GoogleEventId = meet.EventId,
                NotifyOnDelete = NotifyStudentOnDelete
            };

            _context.LessonPlans.Add(plan);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        // EDIT START
        public async Task<IActionResult> OnPostEditStartAsync(int id, DateTime Week)
        {
            this.Week = Week;
            ComputeWeek();
            await LoadDropdownsAsync();

            EditPlan = await _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .FirstOrDefaultAsync(p => p.Id == id);

            Plans = await _context.LessonPlans
                .Include(p => p.Student)
                .Include(p => p.Subject)
                .Include(p => p.SubjectTopic)
                .Where(p => p.Date >= StartOfWeek && p.Date <= EndOfWeek)
                .OrderBy(p => p.Date)
                .ThenBy(p => p.Start)
                .ToListAsync();

            return Page();
        }

        // UPDATE
        public async Task<IActionResult> OnPostUpdateAsync(
            int id,
            int StudentId,
            int SubjectId,
            int? SubjectTopicId,
            TimeSpan Start,
            TimeSpan End,
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

            await _context.SaveChangesAsync();

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        // CANCEL EDIT
        public IActionResult OnPostEditCancel(DateTime Week)
        {
            this.Week = Week;
            ComputeWeek();
            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        // DELETE
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
                {
                    await _calendar.DeleteEventAsync(plan.GoogleEventId);
                }

                var html = await _emailBuilder.BuildCanceledAsync(
                    $"{plan.Student.FirstName} {plan.Student.LastName}",
                    plan.Subject?.Name ?? "",
                    plan.SubjectTopic?.Name ?? "",
                    plan.Date,
                    plan.Start,
                    plan.End
                );

                await _email.SendAsync(plan.Student.Email, "Zrušená lekce", html);

                // najít odpovídající odučenou lekci podle studenta, předmětu, data a času
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

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }

        // TEACH
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
                Day = (int)plan.Day,
                Start = plan.Start,
                End = plan.End,
                Hours = hours,
                IsTaught = true,
                MeetLink = plan.MeetLink
            });

            plan.IsTaught = true;
            await _context.SaveChangesAsync();

            return RedirectToPage(new { week = StartOfWeek.ToString("yyyy-MM-dd") });
        }
    }
}
