using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vyuka.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{LastName} {FirstName}";

        public int? Age { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? School { get; set; }
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }

        public List<StudentSubject> StudentSubjects { get; set; } = new();

        public int? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [NotMapped] public double TaughtHours { get; set; }
        [NotMapped] public double PaidHours { get; set; }
        [NotMapped] public double RemainingHours { get; set; }

        // ❌ TOTO SMAZAT:
        // public int? ParentId { get; set; }

        public string? Level { get; set; }

        [MaxLength(50)]
        public string? PreferredTime { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        public int Credit { get; set; }

        public List<Lesson> Lessons { get; set; } = new();
        public List<LessonPlan> LessonPlans { get; set; } = new();
    }

}
