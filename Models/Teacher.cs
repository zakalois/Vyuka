using System.ComponentModel.DataAnnotations.Schema;

namespace Vyuka.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public AppUser User { get; set; }

        public bool IsActive { get; set; } = true;

        // Jméno učitele se bere z AspNetUsers
        [NotMapped]
        public string FullName => $"{User?.FirstName} {User?.LastName}";

        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
