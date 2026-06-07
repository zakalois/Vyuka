using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vyuka.Models
{
    public class Student
    {
        public int Id { get; set; }

        // ⭐ Toto je vazba na AspNetUsers (účet studenta)
        public string? UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{LastName} {FirstName}";

        public int? Age { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? School { get; set; }
        public string? Address { get; set; }

        public string? ParentFirstName { get; set; }
        public string? ParentLastName { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentEmail { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? ArchivedAt { get; set; }
        public string? ArchiveReason { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }

        public List<StudentSubject> StudentSubjects { get; set; } = new();

        // ⭐ SPRÁVNÁ vazba na Teacher (int FK)
        public int? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        // ⭐ Výpočtové vlastnosti
        [NotMapped] public double TaughtHours { get; set; }
        [NotMapped] public double PaidHours { get; set; }
        [NotMapped] public double RemainingHours { get; set; }

        public string? Level { get; set; }

        [MaxLength(50)]
        public string? PreferredTime { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public List<Lesson> Lessons { get; set; } = new();
        public List<LessonPlan> LessonPlans { get; set; } = new();
    }
}
