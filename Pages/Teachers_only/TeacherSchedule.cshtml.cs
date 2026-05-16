using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vyuka.Models;

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

        public ScheduleModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Week { get; set; }

        public int CurrentWeek { get; set; }
        public int PreviousWeek { get; set; }
        public int NextWeek { get; set; }

        public DateTime StartOfWeek { get; set; }
        public DateTime EndOfWeek { get; set; }

        [BindProperty]
        public NewLessonInput NewLesson { get; set; } = new();

        [BindProperty]
        public UnifiedLesson? EditingLesson { get; set; }

        [BindProperty]
        public bool EditingIsPlan { get; set; }

        public List<Student> Students { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
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

        private async Task LoadStudentsAndSubjects()
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
        }

        private async Task LoadSchedule()
        {
            await LoadStudentsAndSubjects();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);

            var baseMonday = new DateTime(2000, 1, 3);
            StartOfWeek = baseMonday.AddDays(7 * Week);
            EndOfWeek = StartOfWeek.AddDays(6);

            var plans = await _context.LessonPlans
                .Where(lp => lp.Student.TeacherId == teacher.Id &&
                             lp.Date >= StartOfWeek && lp.Date <= EndOfWeek)
                .Include(lp => lp.Student)
                .Include(lp => lp.Subject)
                .ToListAsync();

            var lessons = await _context.Lessons
                .Where(l => l.TeacherId == teacher.Id &&
                            l.Date >= StartOfWeek && l.Date <= EndOfWeek)
                .Include(l => l.Student)
                .Include(l => l.Subject)
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
                                Date = lp.Date,
                                Start = lp.Start,
                                End = lp.End,
                                Student = $"{lp.Student.LastName} {lp.Student.FirstName}",
                                StudentId = lp.StudentId,
                                Subject = lp.Subject.Name,
                                SubjectId = lp.SubjectId,
                                MeetLink = lp.MeetLink,
                                Color = GetStudentColor(lp.Student)
                            })
                        .Concat(
                            lessons
                                .Where(l => l.Date.Date == date.Date)
                                .Select(l => new UnifiedLesson
                                {
                                    LessonId = l.Id,
                                    Date = l.Date,
                                    Start = l.Start,
                                    End = l.End,
                                    Student = $"{l.Student.LastName} {l.Student.FirstName}",
                                    StudentId = l.StudentId,
                                    Subject = l.Subject.Name,
                                    SubjectId = l.SubjectId,
                                    MeetLink = l.MeetLink,
                                    Color = GetStudentColor(l.Student)
                                })
                        )
                        .OrderBy(x => x.Start)
                        .ToList()
                });
            }
        }

        public async Task<IActionResult> OnGetAsync(int? editId, bool? isPlan)
        {
            // DŮLEŽITÉ: jinak ti model binding přepíše EditingLesson na null
            ModelState.Clear();

            if (Week == 0)
            {
                var todayMonday = DateTime.Today.StartOfWeek(DayOfWeek.Monday);
                var baseMonday = new DateTime(2000, 1, 3);
                Week = (int)((todayMonday - baseMonday).TotalDays / 7);
            }

            CurrentWeek = Week;
            PreviousWeek = Week - 1;
            NextWeek = Week + 1;

            if (editId != null && isPlan == true)
            {
                var plan = await _context.LessonPlans
                    .Include(lp => lp.Student)
                    .Include(lp => lp.Subject)
                    .FirstOrDefaultAsync(lp => lp.Id == editId.Value);

                if (plan != null)
                {
                    EditingIsPlan = true;

                    EditingLesson = new UnifiedLesson
                    {
                        LessonPlanId = plan.Id,
                        Date = plan.Date,
                        Start = plan.Start,
                        End = plan.End,
                        StudentId = plan.StudentId,
                        SubjectId = plan.SubjectId,
                        MeetLink = plan.MeetLink
                    };
                }
            }

            await LoadSchedule();
            return Page();
        }



        public async Task<IActionResult> OnPostEditStartAsync(int id, bool isPlan)
        {
            EditingIsPlan = isPlan;

            if (isPlan)
            {
                var plan = await _context.LessonPlans
                    .Include(lp => lp.Student)
                    .Include(lp => lp.Subject)
                    .FirstOrDefaultAsync(lp => lp.Id == id);

                if (plan != null)
                {
                    EditingLesson = new UnifiedLesson
                    {
                        LessonPlanId = plan.Id,
                        Date = plan.Date,
                        Start = plan.Start,
                        End = plan.End,
                        StudentId = plan.StudentId,
                        SubjectId = plan.SubjectId,
                        MeetLink = plan.MeetLink
                    };
                }
            }

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week = Week });
        }


        public async Task<IActionResult> OnPostEditSaveAsync(
    int id, bool isPlan,
    DateTime date, TimeSpan start, TimeSpan end,
    int studentId, int subjectId, string meetLink)
        {
            if (isPlan)
            {
                var plan = await _context.LessonPlans.FirstOrDefaultAsync(lp => lp.Id == id);

                if (plan != null)
                {
                    plan.Date = date;
                    plan.Start = start;
                    plan.End = end;
                    plan.StudentId = studentId;
                    plan.SubjectId = subjectId;
                    plan.MeetLink = meetLink;

                    await _context.SaveChangesAsync();

                    var baseMonday = new DateTime(2000, 1, 3);
                    var newWeek = (int)((date.StartOfWeek(DayOfWeek.Monday) - baseMonday).TotalDays / 7);

                    return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week = newWeek });
                }
            }

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week = Week });
        }



        public async Task<IActionResult> OnPostDeleteLessonAsync(int id, int week)
        {
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == id);

            if (lesson != null)
            {
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });
        }


        public async Task<IActionResult> OnPostDeletePlanAsync(int id, int week)
        {
            var plan = await _context.LessonPlans.FirstOrDefaultAsync(lp => lp.Id == id);

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
                .FirstOrDefaultAsync(lp => lp.Id == id.Value);

            if (plan != null)
            {
                var teacherId = plan.Student?.TeacherId ?? 0;

                var lesson = new Lesson
                {
                    Date = plan.Date,
                    Start = plan.Start,
                    End = plan.End,
                    StudentId = plan.StudentId,
                    SubjectId = plan.SubjectId,
                    TeacherId = teacherId,
                    MeetLink = plan.MeetLink,
                    IsTaught = true
                };

                _context.Lessons.Add(lesson);
                _context.LessonPlans.Remove(plan);

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Teachers_Only/TeacherSchedule", new { week });
        }
    }


    public class NewLessonInput
    {
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
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

        public string Color { get; set; } = "#ffffff";
    }


    public class DaySchedule
    {
        public string Day { get; set; }
        public DateTime Date { get; set; }
        public List<UnifiedLesson> Lessons { get; set; } = new();
    }
}
