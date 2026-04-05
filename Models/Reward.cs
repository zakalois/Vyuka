using Microsoft.EntityFrameworkCore;

namespace Vyuka.Models
{
    public class Reward
    {
        public int Id { get; set; }

        [Precision(18, 2)]
        public decimal Rate { get; set; }

        [Precision(18, 2)]
        public decimal Amount { get; set; }

        public bool Paid { get; set; } = false;

        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
    }
}