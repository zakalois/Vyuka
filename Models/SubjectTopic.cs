namespace Vyuka.Models
{
    public class SubjectTopic
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        // Volitelné, ale doporučené:
        public ICollection<LessonPlan> LessonPlans { get; set; } = new List<LessonPlan>();
    }
}