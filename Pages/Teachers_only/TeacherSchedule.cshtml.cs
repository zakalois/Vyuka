using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vyuka.Models;
using Vyuka.Services;

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

    public class TeacherScheduleModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly GoogleCalendarService _calendar;
        private readonly IEmailService _email;
        private readonly LessonEmailBuilder _emailBuilder;

        public TeacherScheduleModel(
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

        [BindProperty(SupportsGet = true)]
        public int Week { get; set; }

        [BindProperty]
        public NewLessonInput NewLesson { get; set; } = new();

        [BindProperty]
        public UnifiedLesson? EditingLesson { get; set; }

        public int CurrentWeek { get; set; }
        public int PreviousWeek { get; set; }
        public int NextWeek { get; set; }

        public DateTime StartOfWeek { get; set; }
        public DateTime EndOfWeek { get; set; }

        public List<Student> Students { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<SubjectTopic> Topics { get; set; } = new();

        public List<DaySchedule> WeeklySchedule { get; set; } = new();

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

        private async Task LoadStudentsSubjectsTopics()
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

            Topics = await _context.SubjectTopics
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        private async Task LoadSchedule()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            var baseMonday = new DateTime(2000, 1, 3);
            StartOfWeek = baseMonday.AddDays(7 * Week);
            EndOfWeek = StartOfWeek.AddDays(6);

            var plans = await _context.LessonPlans
                .Include(lp => lp.Student)
                .Include(lp => lp.Subject)
                .Include(lp => lp.SubjectTopic)
                .Where(lp =>
                    lp.Student.TeacherId == teacher.Id &&
                    lp.Date >= StartOfWeek &&
                    lp.Date <= EndOfWeek)
                .ToListAsync();

            var lessons = await _context.Lessons
                .Include(l => l.Student)
                .Include(l => l.Subject)
                .Where(l =>
                    l.TeacherId == teacher.Id &&
                    l.Date >= StartOfWeek &&
                    l.Date <= EndOfWeek)
                .ToListAsync();

            WeeklySchedule = new();

            string[] order = { "Pondělí", "Úterý", "Středa", "Čtvrtek", "Pátek", "Sobota", "Neděle" };

            for (int i = 0; i < order.Length; i++)
            {
                var date = StartOfWeek.AddDays(i);

                WeeklySchedule.Add(new DaySchedule
                {
                    Day = order[i],
                    Date = date,
                    Lessons =
                        plans
    .Where(p => p.Date.Date == date.Date)
    .Select(lp => new UnifiedLesson
    {
        LessonPlanId = lp.Id,
        LessonId = null,
        Date = lp.Date,
        Start = lp.Start,
        End = lp.End,
        Student = $"{lp.Student.LastName} {lp.Student.FirstName}",
        StudentId = lp.StudentId,
        Subject = lp.Subject.Name,
        SubjectId = lp.SubjectId,
        SubjectTopicId = lp.SubjectTopicId,
        SubjectTopic = lp.SubjectTopic?.Name,
        MeetLink = lp.MeetLink,
        Color = GetStudentColor(lp.Student),
        IsTaught = lp.IsTaught
    })


//                        .Concat(
//    lessons
//        .Where(l => l.Date.Date == date.Date)
//        .Select(l => new UnifiedLesson
//        {
//            LessonId = l.Id,
//            LessonPlanId = null,
//            Date = l.Date,
//            Start = l.Start,
//            End = l.End,
//            Student = $"{l.Student.LastName} {l.Student.FirstName}",
//            StudentId = l.StudentId,
//            Subject = l.Subject.Name,
//            SubjectId = l.SubjectId,
//            SubjectTopicId = null,
//            SubjectTopic = null,
//            MeetLink = l.MeetLink,
//            Color = GetStudentColor(l.Student),
//            IsTaught = l.IsTaught
//        })
//)

                        .OrderBy(x => x.Start)
                        .ToList()
                });
            }
        }

        public async Task<IActionResult> OnGetAsync(int? editId)
        {
            await LoadStudentsSubjectsTopics();

            if (Week <= 0)
            {
                var todayMonday = DateTime.Today.StartOfWeek(DayOfWeek.Monday);
                var baseMonday = new DateTime(2000, 1, 3);
                Week = (int)((todayMonday - baseMonday).TotalDays / 7);
            }

            CurrentWeek = Week;
            PreviousWeek = Week - 1;
            NextWeek = Week + 1;

            if (editId.HasValue)
            {
                // vždy se pokusíme načíst plánovanou hodinu
                var plan = await _context.LessonPlans
                    .Include(lp => lp.Student)
                    .Include(lp => lp.Subject)
                    .Include(lp => lp.SubjectTopic)
                    .FirstOrDefaultAsync(lp => lp.Id == editId.Value);

                if (plan != null)
                {
                    EditingLesson = new UnifiedLesson
                    {
                        LessonPlanId = plan.Id,
                        LessonId = null,
                        StudentId = plan.StudentId,
                        SubjectId = plan.SubjectId,
                        SubjectTopicId = plan.SubjectTopicId,
                        Date = plan.Date,
                        Start = plan.Start,
                        End = plan.End
                    };
                }
                else
                {
                    // pokud by někdy přišlo ID odučené hodiny, editaci ignorujeme
                    EditingLesson = null;
                }
            }

            await LoadSchedule();
            return Page();
        }

        // ULOŽENÍ ZMĚN PO EDITACI – pouze plánovaná hodina
        public async Task<IActionResult> OnPostEditSaveAsync(
            int id,
            int studentId,
            int subjectId,
            int? subjectTopicId,
            DateTime date,
            string start,
            string end,
            int week)
        {
            Console.WriteLine("DEBUG: Handler běží");
            Console.WriteLine($"DEBUG: id={id}, studentId={studentId}, subjectId={subjectId}");
            Console.WriteLine($"DEBUG: date={date}, start={start}, end={end}");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });

            var startTs = TimeSpan.ParseExact(start, @"hh\:mm", null);
            var endTs = TimeSpan.ParseExact(end, @"hh\:mm", null);

            var plan = await _context.LessonPlans
                .Include(lp => lp.Student)
                .Include(lp => lp.Subject)
                .FirstOrDefaultAsync(lp => lp.Id == id);

            if (plan == null)
                return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });

            Console.WriteLine("DEBUG: Před změnou:");
            Console.WriteLine($"Plan.Id={plan.Id}");
            Console.WriteLine($"Start={plan.Start}, End={plan.End}, Date={plan.Date}");
            Console.WriteLine($"StudentId={plan.StudentId}, SubjectId={plan.SubjectId}");

            plan.TeacherId = teacher.Id;
            plan.StudentId = studentId;
            plan.SubjectId = subjectId;
            plan.SubjectTopicId = subjectTopicId;
            plan.Date = date;
            plan.Start = startTs;
            plan.End = endTs;

            var student = await _context.Students.FindAsync(studentId);
            var subject = await _context.Subjects.FindAsync(subjectId);

            var meet = await _calendar.CreateMeetEventAsync(
                date + startTs,
                date + endTs,
                $"{subject.Name} – {student.LastName} {student.FirstName}",
                student.Email,
                "zakalois@gmail.com",
                student.FirstName,
                student.LastName,
                "#90CAF9"
            );

            plan.MeetLink = meet.MeetLink;
            plan.GoogleEventId = meet.EventId;

            await _context.SaveChangesAsync();

            Console.WriteLine("DEBUG: Po změně:");
            Console.WriteLine($"Start={plan.Start}, End={plan.End}, Date={plan.Date}");
            Console.WriteLine($"StudentId={plan.StudentId}, SubjectId={plan.SubjectId}");

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });
        }

        public async Task<IActionResult> OnPostAddLessonAsync(int week)
        {
            await LoadStudentsSubjectsTopics();

            if (NewLesson.Date == default || NewLesson.Start == default || NewLesson.End == default)
                return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });

            var student = await _context.Students.FindAsync(NewLesson.StudentId);
            if (student == null)
                return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });

            var subject = await _context.Subjects.FindAsync(NewLesson.SubjectId);
            if (subject == null)
                return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });

            var meet = await _calendar.CreateMeetEventAsync(
                NewLesson.Date + NewLesson.Start,
                NewLesson.Date + NewLesson.End,
                $"{subject.Name} – {student.LastName} {student.FirstName}",
                student.Email,
                "zakalois@gmail.com",
                student.FirstName,
                student.LastName,
                "#90CAF9"
            );

            var plan = new LessonPlan
            {
                Date = NewLesson.Date,
                Start = NewLesson.Start,
                End = NewLesson.End,
                StudentId = NewLesson.StudentId,
                SubjectId = NewLesson.SubjectId,
                SubjectTopicId = NewLesson.SubjectTopicId,
                MeetLink = meet.MeetLink,
                IsTaught = false,
                TeacherId = student.TeacherId
            };

            _context.LessonPlans.Add(plan);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });
        }

        public async Task<IActionResult> OnPostDeleteLessonAsync(int id, int week)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Student)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson != null)
            {
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });
        }

        public async Task<IActionResult> OnPostDeletePlanAsync(int id, int week)
        {
            var plan = await _context.LessonPlans
                .Include(lp => lp.Student)
                .FirstOrDefaultAsync(lp => lp.Id == id);

            if (plan != null)
            {
                _context.LessonPlans.Remove(plan);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });
        }

        public async Task<IActionResult> OnPostTeachLessonAsync(int? id, int week)
        {
            if (id == null)
                return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });

            var plan = await _context.LessonPlans
                .Include(lp => lp.Student)
                .Include(lp => lp.Subject)
                .FirstOrDefaultAsync(lp => lp.Id == id);

            if (plan != null)
            {
                // 1️⃣ Najdeme skutečného učitele (přihlášeného)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

                if (teacher == null)
                    return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });

                // 2️⃣ Označíme plán jako odučený
                plan.IsTaught = true;

                // 3️⃣ Převod TimeSpan (bez stringů!)
                var start = plan.Start;
                var end = plan.End;

                var duration = end - start;

                // 4️⃣ Vytvoříme Lesson se SPRÁVNÝM TeacherId
                var lesson = new Lesson
                {
                    Date = plan.Date.Date,   // 🔥 jistota, že se den neposune
                    Start = start,
                    End = end,
                    StudentId = plan.StudentId,
                    SubjectId = plan.SubjectId,
                    SubjectTopicId = plan.SubjectTopicId,
                    TeacherId = teacher.Id,  // 🔥 KLÍČOVÁ OPRAVA
                    MeetLink = plan.MeetLink,
                    IsTaught = true,

                    Day = plan.Date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)plan.Date.DayOfWeek,
                    Hours = (decimal)duration.TotalHours
                };

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });
        }


    }

    public class NewLessonInput
    {
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public int? SubjectTopicId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
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
        public int? SubjectTopicId { get; set; }
        public string? SubjectTopic { get; set; }

        public string Color { get; set; } = "#ffffff";
    }

    public class DaySchedule
    {
        public string Day { get; set; }
        public DateTime Date { get; set; }
        public List<UnifiedLesson> Lessons { get; set; } = new();
    }
}
