namespace Vyuka.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public AppUser User { get; set; }

        public bool IsActive { get; set; } = true;

        public string FullName => $"{User?.FirstName} {User?.LastName}";
    }
}
