namespace Vyuka.Models
{
    public class StudentAdminViewModel
    {
        public Student Student { get; set; }
        public Parent Parent { get; set; }

        public double TaughtHours { get; set; }
        public double PaidHours { get; set; }
        public double RemainingHours { get; set; }
    }
}
