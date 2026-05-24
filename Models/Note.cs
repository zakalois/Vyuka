using Vyuka.Models;

public class Note
{
    public int Id { get; set; }

    public int? StudentId { get; set; }
    public int? TeacherId { get; set; }

    public string Text { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; }

    public Student Student { get; set; }
    public Teacher Teacher { get; set; }
}
