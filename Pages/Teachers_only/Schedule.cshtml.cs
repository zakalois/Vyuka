using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vyuka.Models;
using Vyuka.Pages.Admin;
using Vyuka.Services;
using static Vyuka.Pages.Teachers_only.DashboardModel;

namespace Vyuka.Pages.Teachers_only
{
    public static class DateExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }

    public class ScheduleModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly GoogleCalendarService _calendar;

        // ⭐ Používáme rozhraní, protože v Program.cs je IEmailService → EmailService
        private readonly IEmailService _email;
        private readonly LessonPlanEmailBuilder _emailBuilder;

        public ScheduleModel(
            AppDbContext context,
            GoogleCalendarService calendar,
            IEmailService email,
            LessonPlanEmailBuilder emailBuilder)
        {
            _context = context;
            _calendar = calendar;
            _email = email;
            _emailBuilder = emailBuilder;
        }

        [BindProperty]
        public NewLessonInput NewLesson { get; set; } = new();

        public Lesson? EditingLesson { get; set; }
        public bool EditingIsPlan { get; set; }

        public List<Student> Students { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();

        public List<DaySchedule> WeeklySchedule { get; set; } = new();
        public string WeekLabel { get; set; } = "";
        public int PreviousWeek { get; set; }
        public int NextWeek { get; set; }
        public DateTime WeekStart { get; set; }

        private readonly string[] _colors = new[]
        {
            "#FFB3BA", "#FFDFBA", "#FFFFBA", "#BAFFC9", "#BAE1FF",
            "#e6ccff", "#ffccf2", "#ffd9cc", "#ccffe6", "#e6f7ff",
            "#ffcccc", "#ffe6cc", "#ffffcc", "#e6ffcc", "#ccffff",
            "#ffccff", "#ffebcc", "#e6ffe6", "#e6e6ff", "#ffe6f2",
            "#ffb3d9", "#d9b3ff", "#b3d9ff", "#b3ffd9", "#ffd9b3"
        };

        private string GetStudentColor(Student s)
        {
            int index = Math.Abs(s.Id.GetHashCode()) % _colors.Length;
            return _colors[index];
        }

        public async Task<IActionResult> OnGetAsync(int? week)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            Students = await _context.Students
                .Where(s => s.TeacherId == teacher.Id)
                .OrderBy(s => s.LastName)
                .ToListAsync();

            Subjects = await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();

            var monday = DateTime.Today.StartOfWeek(DayOfWeek.Monday);
            if (week.HasValue) monday = monday.AddDays(7 * week.Value);

            WeekStart = monday;
            WeekLabel = $"{monday:dd.MM.yyyy} – {monday.AddDays(6):dd.MM.yyyy}";
            PreviousWeek = (week ?? 0) - 1;
            NextWeek = (week ?? 0) + 1;

            if (NewLesson.Date == default)
                NewLesson.Date = monday;

            if (NewLesson.Start == default)
                NewLesson.Start = new TimeSpan(8, 0, 0);

            if (NewLesson.End == default)
                NewLesson.End = new TimeSpan(9, 0, 0);

            var plans = await _context.LessonPlans
                .Where(lp => lp.Student.TeacherId == teacher.Id &&
                             lp.Date >= monday && lp.Date < monday.AddDays(7))
                .Include(lp => lp.Student)
                .Include(lp => lp.Subject)
                .ToListAsync();

            var lessons = await _context.Lessons
                .Where(l => l.TeacherId == teacher.Id &&
                            l.Date >= monday &&
                            l.Date < monday.AddDays(7))
                .Include(l => l.Student)
                .Include(l => l.Subject)
                .ToListAsync();

            plans = plans
                .Where(lp => !lessons.Any(l =>
                    l.Date == lp.Date &&
                    l.Start == lp.Start &&
                    l.End == lp.End &&
                    l.StudentId == lp.StudentId))
                .ToList();

            var unified = new List<UnifiedLesson>();

            unified.AddRange(plans.Select(lp => new UnifiedLesson
            {
                Date = lp.Date,
                Start = lp.Start,
                End = lp.End,
                Student = $"{lp.Student.LastName} {lp.Student.FirstName}",
                StudentId = lp.Student.Id,
                Subject = lp.Subject.Name,
                SubjectId = lp.Subject.Id,
                IsTaught = lp.IsTaught,
                MeetLink = lp.MeetLink,
                LessonPlanId = lp.Id,
                Color = GetStudentColor(lp.Student)
            }));

            unified.AddRange(lessons.Select(l => new UnifiedLesson
            {
                Date = l.Date,
                Start = l.Start,
                End = l.End,
                Student = $"{l.Student.LastName} {l.Student.FirstName}",
                StudentId = l.Student.Id,
                Subject = l.Subject.Name,
                SubjectId = l.Subject.Id,
                IsTaught = l.IsTaught,
                MeetLink = l.MeetLink,
                LessonId = l.Id,
                Color = GetStudentColor(l.Student)
            }));

            string[] order = { "Pondělí", "Úterý", "Středa", "Čtvrtek", "Pátek", "Sobota", "Neděle" };

            for (int i = 0; i < order.Length; i++)
            {
                var date = monday.AddDays(i);
                WeeklySchedule.Add(new DaySchedule
                {
                    Day = order[i],
                    Date = date,
                    Lessons = unified
                        .Where(u => u.Date.Date == date.Date)
                        .OrderBy(u => u.Start)
                        .ToList()
                });
            }

            return Page();
        }

        private async Task<(Lesson? lesson, LessonPlan? plan)> FindLessonOrPlanAsync(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson != null)
                return (lesson, null);

            var plan = await _context.LessonPlans.FindAsync(id);
            return (null, plan);
        }

        // ⭐ FINÁLNÍ VERZE – vytváří Meet + posílá email + ukládá
        public async Task<IActionResult> OnPostAddLesson()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
            var teacherUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == teacher.UserId);

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == NewLesson.StudentId);
            var studentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == student.UserId);

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == NewLesson.SubjectId);

            if (teacher == null || teacherUser == null ||
                student == null || studentUser == null ||
                subject == null)
            {
                return RedirectToPage();
            }

            var startDateTime = NewLesson.Date.Date + NewLesson.Start;
            var endDateTime = NewLesson.Date.Date + NewLesson.End;

            var hexColor = "#2196F3";

            // ⭐ Google Meet
            var meet = await _calendar.CreateMeetEventAsync(
                startDateTime,
                endDateTime,
                subject.Name,
                studentUser.Email,
                teacherUser.Email,
                student.FirstName,
                student.LastName,
                hexColor
            );

            // ⭐ Odeslání tvé šablony s logem
            var html = await _emailBuilder.BuildAsync(
                $"{student.FirstName} {student.LastName}",
                subject.Name,
                "",
                NewLesson.Date,
                NewLesson.Start,
                meet.MeetLink
            );

            await _email.SendAsync(
                studentUser.Email,
                "Plánovaná lekce",
                html
            );

            // ⭐ Uložení do DB
            var plan = new LessonPlan
            {
                StudentId = NewLesson.StudentId,
                SubjectId = NewLesson.SubjectId,
                Date = NewLesson.Date,
                Start = NewLesson.Start,
                End = NewLesson.End,
                IsTaught = false,
                MeetLink = meet.MeetLink,
                NotifyOnDelete = false
            };

            _context.LessonPlans.Add(plan);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditStartAsync(int id, DateTime Week)
        {
            (Lesson? lesson, LessonPlan? plan) = await FindLessonOrPlanAsync(id);

            if (lesson != null)
            {
                EditingLesson = lesson;
                EditingIsPlan = false;
            }
            else if (plan != null)
            {
                EditingLesson = new Lesson
                {
                    Id = plan.Id,
                    StudentId = plan.StudentId,
                    SubjectId = plan.SubjectId,
                    Date = plan.Date,
                    Start = plan.Start,
                    End = plan.End,
                    MeetLink = plan.MeetLink
                };
                EditingIsPlan = true;
            }

            int weekOffset = (int)((Week.StartOfWeek(DayOfWeek.Monday) -
                                    DateTime.Today.StartOfWeek(DayOfWeek.Monday)).TotalDays / 7);

            await OnGetAsync(weekOffset);
            return Page();
        }

        public async Task<IActionResult> OnPostEditSaveAsync(
            int id,
            bool isPlan,
            DateTime date,
            TimeSpan start,
            TimeSpan end,
            int subjectId,
            int studentId,
            string? meetLink)
        {
            if (isPlan)
            {
                var plan = await _context.LessonPlans.FindAsync(id);
                if (plan == null) return RedirectToPage();

                plan.Date = date;
                plan.Start = start;
                plan.End = end;
                plan.SubjectId = subjectId;
                plan.StudentId = studentId;
                plan.MeetLink = meetLink;
            }
            else
            {
                var lesson = await _context.Lessons.FindAsync(id);
                if (lesson == null) return RedirectToPage();

                lesson.Date = date;
                lesson.Start = start;
                lesson.End = end;
                lesson.SubjectId = subjectId;
                lesson.StudentId = studentId;
                lesson.MeetLink = meetLink;
            }

            await _context.SaveChangesAsync();

            int weekOffset = (int)((date.StartOfWeek(DayOfWeek.Monday) -
                                    DateTime.Today.StartOfWeek(DayOfWeek.Monday)).TotalDays / 7);

            return RedirectToPage(new { week = weekOffset });
        }

        public async Task<IActionResult> OnPostDeleteLessonAsync(int id, DateTime Week)
        {
            (Lesson? lesson, LessonPlan? plan) = await FindLessonOrPlanAsync(id);

            if (lesson != null) _context.Lessons.Remove(lesson);
            else if (plan != null) _context.LessonPlans.Remove(plan);

            await _context.SaveChangesAsync();

            int weekOffset = (int)((Week.StartOfWeek(DayOfWeek.Monday) -
                                    DateTime.Today.StartOfWeek(DayOfWeek.Monday)).TotalDays / 7);

            return RedirectToPage(new { week = weekOffset });
        }

        public async Task<IActionResult> OnPostTeachLessonAsync(int id, DateTime Week)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            (Lesson? lesson, LessonPlan? plan) = await FindLessonOrPlanAsync(id);

            if (lesson != null)
            {
                lesson.IsTaught = true;
            }
            else if (plan != null)
            {
                var newLesson = new Lesson
                {
                    StudentId = plan.StudentId,
                    SubjectId = plan.SubjectId,
                    Date = plan.Date,
                    Start = plan.Start,
                    End = plan.End,
                    MeetLink = plan.MeetLink,
                    IsTaught = true,
                    TeacherId = teacher.Id
                };

                _context.Lessons.Add(newLesson);
                _context.LessonPlans.Remove(plan);
            }

            await _context.SaveChangesAsync();

            int weekOffset = (int)((Week.StartOfWeek(DayOfWeek.Monday) -
                                    DateTime.Today.StartOfWeek(DayOfWeek.Monday)).TotalDays / 7);

            return RedirectToPage(new { week = weekOffset });
        }
    }

    public class NewLessonInput
    {
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public bool SendCancelNotification { get; set; }
        public string? MeetLink { get; set; }
    }

    public class UnifiedLesson
    {
        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }

        public string Student { get; set; }
        public int StudentId { get; set; }

        public string Subject { get; set; }
        public int SubjectId { get; set; }

        public bool IsTaught { get; set; }
        public string? MeetLink { get; set; }

        public int? LessonId { get; set; }
        public int? LessonPlanId { get; set; }

        public string Color { get; set; } = "#ffffff";
    }

    public class DaySchedule
    {
        public string Day { get; set; }
        public DateTime Date { get; set; }
        public List<UnifiedLesson> Lessons { get; set; } = new();
    }
}
