using System.ComponentModel.DataAnnotations.Schema;

namespace Vyuka.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        // ⭐ Vazba na AspNetUsers (účet učitele)
        public string UserId { get; set; }
        public AppUser User { get; set; }

        public bool IsActive { get; set; } = true;

        // ⭐ Jméno učitele se bere z AspNetUsers
        [NotMapped]
        public string FullName => $"{User?.FirstName} {User?.LastName}";

        // ⭐ Správná vazba: Teacher má mnoho Studentů
        public ICollection<Student> Students { get; set; } = new List<Student>();

        // ⭐ Učitel má také mnoho Lesson
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
