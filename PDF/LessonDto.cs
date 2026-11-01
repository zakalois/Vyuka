using QuestPDF.Fluent;

namespace Vyuka.Models
{
    public class LessonDto
    {
        public int Order { get; set; }      // číslo hodiny
        public DateTime Date { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public int Hours { get; set; }      // celé hodiny
        public string Type { get; set; } = "";
    }
}

