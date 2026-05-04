using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Reflection;

namespace Vyuka.Models
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Lesson> Lessons { get; set; }

        // ✔ Přejmenováno – žádný konflikt s AspNetUsers
        public DbSet<AppUser> AppUsers { get; set; }
        
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<SubjectTopic> SubjectTopics { get; set; }
        public DbSet<LessonPlan> LessonPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            base.OnModelCreating(modelBuilder);

            var pageModelType = typeof(Microsoft.AspNetCore.Mvc.RazorPages.PageModel);
            var pageModels = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => pageModelType.IsAssignableFrom(t));

            foreach (var pm in pageModels)
                modelBuilder.Ignore(pm);

            modelBuilder.Entity<StudentSubject>()
                .HasKey(ss => new { ss.StudentId, ss.SubjectId });

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 0);

            modelBuilder.Entity<Payment>()
                .Property(p => p.PricePerHour)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.HoursPurchased)
                .HasPrecision(18, 2);
        }

    }
}
