using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Vyuka.Models
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<SubjectTopic> SubjectTopics { get; set; }
        public DbSet<LessonPlan> LessonPlans { get; set; }
        public DbSet<Teacher> Teachers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("dbo");

            // Ignorujeme PageModel třídy
            var pageModelType = typeof(Microsoft.AspNetCore.Mvc.RazorPages.PageModel);
            var pageModels = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => pageModelType.IsAssignableFrom(t));

            foreach (var pm in pageModels)
                modelBuilder.Ignore(pm);

            // ⭐ Explicitní mapování MeetLink – TOTO JE TEN CHYBĚJÍCÍ KUS
            modelBuilder.Entity<Lesson>()
                .Property(l => l.MeetLink)
                .HasColumnName("MeetLink")
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            modelBuilder.Entity<LessonPlan>()
                .Property(lp => lp.MeetLink)
                .HasColumnName("MeetLink")
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            // ⭐ Student → Teacher
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Teacher)
                .WithMany(t => t.Students)
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            // ⭐ Lesson → Teacher
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Teacher)
                .WithMany(t => t.Lessons)
                .HasForeignKey(l => l.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // ⭐ Lesson → Student
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Student)
                .WithMany()
                .HasForeignKey(l => l.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ⭐ Lesson → Subject
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Subject)
                .WithMany()
                .HasForeignKey(l => l.SubjectId);

            // ⭐ Lesson → SubjectTopic
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.SubjectTopic)
                .WithMany()
                .HasForeignKey(l => l.SubjectTopicId);

            // StudentSubject PK
            modelBuilder.Entity<StudentSubject>()
                .HasKey(ss => new { ss.StudentId, ss.SubjectId });

            // Payment precision
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
