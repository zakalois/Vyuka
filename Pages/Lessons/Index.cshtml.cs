using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vyuka.Models;

namespace Vyuka.Pages.Lessons
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;

        public IndexModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int SelectedStudentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        public SelectList StudentList { get; set; }

        public decimal SelectedStudentPrepaidHours { get; set; }
        public decimal SelectedStudentTaughtHours { get; set; }
        public decimal SelectedStudentBalance { get; set; }

        public List<LessonRow> LessonOverview { get; set; } = new();
        public LessonRow NextPlannedLesson { get; set; }

        public class LessonRow
        {
            public DateTime Date { get; set; }
            public string Subject { get; set; }
            public string Topic { get; set; }
            public decimal Hours { get; set; }
            public string Type { get; set; }
            public bool IsSeparator { get; set; }
        }

        public void OnGet()
        {
            var students = _db.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            StudentList = new SelectList(students, "Id", "FullName");

            if (SelectedStudentId == 0)
            {
                LessonOverview = new();
                return;
            }

            LoadForSingleStudent();
        }

        private void LoadForSingleStudent()
        {
            // NEFILTROVANÉ – pro výpočet další plánované
            var allPlanned = _db.LessonPlans
                .Include(l => l.Subject)
                .Include(l => l.SubjectTopic)
                .Where(l => l.StudentId == SelectedStudentId)
                .ToList();

            var allTaught = _db.Lessons
                .Include(l => l.Subject)
                .Include(l => l.SubjectTopic)
                .Where(l => l.StudentId == SelectedStudentId)
                .ToList();

            // FILTROVANÉ – jen pro tabulku
            var plannedForTable = allPlanned.ToList();
            var taughtForTable = allTaught.ToList();

            ApplyDateFilter(ref plannedForTable, ref taughtForTable);

            SelectedStudentPrepaidHours = _db.Payments
                .Where(p => p.StudentId == SelectedStudentId)
                .Sum(p => p.HoursPurchased);

            SelectedStudentTaughtHours = taughtForTable.Sum(l => l.Hours);

            SelectedStudentBalance = SelectedStudentPrepaidHours - SelectedStudentTaughtHours;

            BuildLessonOverview(allPlanned, allTaught, plannedForTable, taughtForTable);
        }

        private void ApplyDateFilter(ref List<LessonPlan> planned, ref List<Lesson> taught)
        {
            if (DateFrom.HasValue)
            {
                var from = DateFrom.Value.Date;
                planned = planned.Where(l => l.Date.Date >= from).ToList();
                taught = taught.Where(l => l.Date.Date >= from).ToList();
            }

            if (DateTo.HasValue)
            {
                var to = DateTo.Value.Date;
                planned = planned.Where(l => l.Date.Date <= to).ToList();
                taught = taught.Where(l => l.Date.Date <= to).ToList();
            }
        }

        private void BuildLessonOverview(
            List<LessonPlan> allPlanned,
            List<Lesson> allTaught,
            List<LessonPlan> planned,
            List<Lesson> taught)
        {
            LessonOverview = new List<LessonRow>();

            // ⭐ 1) Pivot = max(dnešní datum, poslední odučená hodina)
            DateTime pivotDate = DateTime.Today;

            if (allTaught.Any())
            {
                var lastTaught = allTaught.Max(t => t.Date.Date);
                if (lastTaught > pivotDate)
                    pivotDate = lastTaught;
            }

            // ⭐ 2) Najít první plánovanou hodinu PO pivotu
            NextPlannedLesson = allPlanned
                .Where(p => p.Date.Date > pivotDate)
                .OrderBy(p => p.Date)
                .Select(l => new LessonRow
                {
                    Date = l.Date,
                    Subject = l.Subject.Name,
                    Topic = l.SubjectTopic?.Name ?? "",
                    Hours = (decimal)(l.End - l.Start).TotalHours,
                    Type = "Plánovaná"
                })
                .FirstOrDefault();

            if (NextPlannedLesson == null)
            {
                NextPlannedLesson = new LessonRow
                {
                    Type = "Student nemá plánovanou hodinu"
                };
            }

            // ⭐ 3) Tabulka – plánované + odučené (FILTROVANÉ)
            var plannedRows = planned
                .Select(l => new LessonRow
                {
                    Date = l.Date,
                    Subject = l.Subject.Name,
                    Topic = l.SubjectTopic?.Name ?? "",
                    Hours = (decimal)(l.End - l.Start).TotalHours,
                    Type = "Plánovaná"
                });

            var taughtRows = taught
                .Select(l => new LessonRow
                {
                    Date = l.Date,
                    Subject = l.Subject.Name,
                    Topic = l.SubjectTopic?.Name ?? "",
                    Hours = l.Hours,
                    Type = "Odučená"
                });

            LessonOverview = plannedRows
                .Concat(taughtRows)
                .OrderByDescending(r => r.Date) // nejnovější nahoře
                .ToList();
        }
    }
}
