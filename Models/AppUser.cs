using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vyuka.Models
{
    [Table("AppUsers", Schema = "dbo")]
    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string Name => $"{FirstName} {LastName}";

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Role { get; set; }

        public string PasswordHash { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        public string? PhotoPath { get; set; }
    }

}
