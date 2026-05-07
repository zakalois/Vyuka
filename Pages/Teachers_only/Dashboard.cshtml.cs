using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vyuka.Pages.Teachers_only
{
    public class DashboardModel : PageModel
    {
        public List<LessonInfo> TodayLessons { get; set; } = new();
        public int StudentCount { get; set; }
        public int LowCreditCount { get; set; }
        public List<DaySchedule> WeeklySchedule { get; set; } = new();
        public List<string> Notifications { get; set; } = new();

        public void OnGet()
        {
            // MOCK DATA – později nahradíme databází

            TodayLessons = new List<LessonInfo>
            {
                new LessonInfo("08:00", "Jan Novák", "Matematika"),
                new LessonInfo("10:00", "Petra Malá", "Fyzika"),
                new LessonInfo("14:00", "Adam Král", "Angličtina")
            };

            StudentCount = 12;
            LowCreditCount = 3;

            WeeklySchedule = new List<DaySchedule>
            {
                new DaySchedule("Pondělí", 2),
                new DaySchedule("Úterý", 1),
                new DaySchedule("Středa", 3),
                new DaySchedule("Čtvrtek", 0),
                new DaySchedule("Pátek", 2)
            };

            Notifications = new List<string>
            {
                "Student Petr zrušil hodinu.",
                "Studentka Eva má nízký kredit."
            };
        }

        public record LessonInfo(string Time, string Student, string Subject);
        public record DaySchedule(string Day, int LessonCount);
    }
}
