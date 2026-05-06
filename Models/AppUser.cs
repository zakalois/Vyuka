using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vyuka.Models
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Fotka uživatele
        public string? PhotoPath { get; set; }

        // Role (pokud ji chceš ukládat navíc)
        public string? Role { get; set; }

        // Spojené jméno
        [NotMapped]
        public string Name => $"{FirstName} {LastName}";
    }
}
