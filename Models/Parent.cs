using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vyuka.Models
{
    [Table("Parents")]
    public class Parent
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        // 1:1 vazba – Parent je dependent
        [ForeignKey(nameof(Student))]
        public int StudentId { get; set; }

        public Student Student { get; set; }
    }

}
