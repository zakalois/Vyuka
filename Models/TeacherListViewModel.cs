public class TeacherListViewModel
{
    public int Id { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Subjects { get; set; }
    public int StudentsCount { get; set; }
    public int HoursThisMonth { get; set; }
    public bool IsActive { get; set; }
}
