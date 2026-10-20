using QuestPDF.Fluent;

namespace Vyuka.PDF
{
    public class LessonDto
    {
        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public decimal Hours { get; set; }
        public string Type { get; set; } // "Odučeno" / "Plánováno"
    }
}
