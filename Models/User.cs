public class AppUser
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }

    // 🔥 Role je ve tvé DB před PasswordHash
    public string Role { get; set; }

    // 🔥 PasswordHash je poslední
    public string PasswordHash { get; set; }
}
