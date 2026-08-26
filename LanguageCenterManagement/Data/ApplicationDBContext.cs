using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<LanguageClass> Classes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>()
                .HasIndex(x => x.StudentCode)
                .IsUnique();

            modelBuilder.Entity<Teacher>()
                .HasIndex(x => x.TeacherCode)
                .IsUnique();

            modelBuilder.Entity<Course>()
                .HasIndex(x => x.CourseCode)
                .IsUnique();

            modelBuilder.Entity<Course>()
                .Property(x => x.TuitionFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LanguageClass>()
                .HasIndex(x => x.ClassCode)
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasIndex(x => x.RoomCode)
                .IsUnique();

            modelBuilder.Entity<Enrollment>()
                .HasIndex(x => new { x.StudentId, x.ClassId })
                .IsUnique();

            modelBuilder.Entity<LanguageClass>()
                .HasOne(x => x.Course)
                .WithMany(x => x.Classes)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LanguageClass>()
                .HasOne(x => x.Teacher)
                .WithMany(x => x.Classes)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(x => x.Class)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Schedule>()
                .HasOne(x => x.Room)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(x => x.Student)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrollment>()
                .HasOne(x => x.Class)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}