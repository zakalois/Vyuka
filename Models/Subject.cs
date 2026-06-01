using System.ComponentModel.DataAnnotations;

namespace Vyuka.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // URL obrázku (zatím)
        public string? ImageUrl { get; set; }

        // Jednoduchý seznam témat oddělený řádky
        public string? Topics { get; set; }
        public List<StudentSubject> StudentSubjects { get; set; } = new();
        public int? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

    }
}