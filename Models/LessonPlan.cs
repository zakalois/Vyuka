using Vyuka.Models;
using System.ComponentModel.DataAnnotations;

public class LessonPlan
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }

    public int SubjectId { get; set; }
    public Subject Subject { get; set; }

    public int? SubjectTopicId { get; set; }
    public SubjectTopic? SubjectTopic { get; set; }

    public DateTime Date { get; set; }
    public bool IsTaught { get; set; }

    public string? MeetLink { get; set; }
    public string? GoogleEventId { get; set; }
    public bool NotifyOnDelete { get; set; }
}
