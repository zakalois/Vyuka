using Microsoft.EntityFrameworkCore;

namespace Vyuka.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }

        public DbSet<SubjectTopic> SubjectTopics { get; set; }
        public DbSet<LessonPlan> LessonPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Klíč pro vazební tabulku
            modelBuilder.Entity<StudentSubject>()
                .HasKey(ss => new { ss.StudentId, ss.SubjectId });

            // ⭐ Amount – celé koruny (bez desetinných míst)
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 0);

            // ⭐ PricePerHour – desetiny (např. 350.5 Kč)
            modelBuilder.Entity<Payment>()
                .Property(p => p.PricePerHour)
                .HasPrecision(18, 2);

            // ⭐ HoursPurchased – desetiny (např. 1.5 h)
            modelBuilder.Entity<Payment>()
                .Property(p => p.HoursPurchased)
                .HasPrecision(18, 2);
        }
    }
}