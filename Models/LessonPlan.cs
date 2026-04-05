using Vyuka.Models;
using System.ComponentModel.DataAnnotations;

public class LessonPlan
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; }

    public DayOfWeek Day { get; set; }   // Po, Út, St, Čt, Pá, So, Ne
    public TimeSpan Start { get; set; }  // 14:00
    public TimeSpan End { get; set; }    // 15:00

    [Required(ErrorMessage = "Musíte vybrat předmět.")]
    public int SubjectId { get; set; }
    public Subject Subject { get; set; }

    public int? SubjectTopicId { get; set; }
    public SubjectTopic? SubjectTopic { get; set; }
    public DateTime Date { get; set; }
    public bool IsTaught { get; set; }
}