using Vyuka.Models;

public class Lesson
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int Day { get; set; }

    public TimeSpan Start { get; set; }

    public TimeSpan End { get; set; }

    // Student
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    // Subject
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    // Topic
    public int? SubjectTopicId { get; set; }
    public SubjectTopic? SubjectTopic { get; set; }

    // NEW: Teacher
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    // Status
    public bool IsTaught { get; set; }
    public string? MeetLink { get; set; }

    public decimal Hours { get; set; }
}
