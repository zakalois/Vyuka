using Vyuka.Models;

public class Lesson
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    // ⭐ NECHÁME DurationMinutes, ale nebudeme ho používat pro výpočet
    public int DurationMinutes { get; set; }

    public string Type { get; set; } = string.Empty;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    // ⭐ NOVÉ — počet hodin lekce (1, 2, 3…)
    public int Hours { get; set; }

    // ⭐ NOVÉ — zda byla lekce odučena
    public bool IsTaught { get; set; }
}