using System.ComponentModel.DataAnnotations.Schema;

namespace Vyuka.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string? UserId { get; set; }   // vazba na AppUser (login)

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

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

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }

        public List<StudentSubject> StudentSubjects { get; set; } = new();

        // ⭐ SPRÁVNĚ – vazba na Teacher
        public int? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        // ⭐ Výpočtové vlastnosti
        [NotMapped]
        public double TaughtHours { get; set; }

        [NotMapped]
        public double PaidHours { get; set; }

        [NotMapped]
        public double RemainingHours { get; set; }
    }
}
