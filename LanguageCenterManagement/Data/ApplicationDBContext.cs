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
        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Question> Questions { get; set; }

        public DbSet<Answer> Answers { get; set; }

        public DbSet<Exam> Exams { get; set; }

        public DbSet<ExamQuestion> ExamQuestions { get; set; }

        public DbSet<ExamResult> ExamResults { get; set; }

        public DbSet<Tuition> Tuitions { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<LearningResult> LearningResults { get; set; }

        public DbSet<Material> Materials { get; set; }

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

            modelBuilder.Entity<Schedule>()
                .HasIndex(s => new
                {
                    s.ClassId,
                    s.StartTime
                });

            modelBuilder.Entity<Schedule>()
                .HasIndex(s => new
                {
                    s.RoomId,
                    s.StartTime
                });

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
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(x => x.Room)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Class)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new
                {
                    e.StudentId,
                    e.ClassId
                })
                .IsUnique();

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Class)
                .WithMany(c => c.Attendances)
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new
                {
                    a.StudentId,
                    a.ClassId,
                    a.AttendanceDate
                })
                .IsUnique();

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamQuestion>()
                .HasOne(eq => eq.Exam)
                .WithMany(e => e.ExamQuestions)
                .HasForeignKey(eq => eq.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamQuestion>()
                .HasOne(eq => eq.Question)
                .WithMany(q => q.ExamQuestions)
                .HasForeignKey(eq => eq.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamResult>()
                .HasOne(er => er.Exam)
                .WithMany(e => e.ExamResults)
                .HasForeignKey(er => er.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamResult>()
                .HasOne(er => er.Student)
                .WithMany(s => s.ExamResults)
                .HasForeignKey(er => er.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tuition>()
                .HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tuition>()
                .HasOne(t => t.Class)
                .WithMany()
                .HasForeignKey(t => t.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Tuition)
                .WithMany()
                .HasForeignKey(p => p.TuitionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LearningResult>()
                .HasOne(l => l.Student)
                .WithMany(s => s.LearningResults)
                .HasForeignKey(l => l.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LearningResult>()
                .HasOne(l => l.Class)
                .WithMany(c => c.LearningResults)
                .HasForeignKey(l => l.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Material>()
                .HasOne(m => m.Class)
                .WithMany(c => c.Materials)
                .HasForeignKey(m => m.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Room>()
                .HasIndex(r => r.RoomCode)
                .IsUnique();

            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new
                {
                    e.StudentId,
                    e.ClassId
                })
                .IsUnique();

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new
                {
                    a.StudentId,
                    a.ClassId,
                    a.AttendanceDate
                })
                .IsUnique();

            modelBuilder.Entity<ExamQuestion>()
                .HasIndex(eq => new
                {
                    eq.ExamId,
                    eq.QuestionId
                })
                .IsUnique();

            modelBuilder.Entity<ExamResult>()
                .HasIndex(er => new
                {
                    er.StudentId,
                    er.ExamId
                })
                .IsUnique();

            modelBuilder.Entity<Course>()
                .Property(c => c.TuitionFee)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Tuition>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Tuition>()
                .Property(t => t.PaidAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Exam>()
                .Property(e => e.MaxScore)
                .HasPrecision(5, 2);

            modelBuilder.Entity<ExamResult>()
                .Property(e => e.Score)
                .HasPrecision(5, 2);

            modelBuilder.Entity<LearningResult>()
                .Property(l => l.AverageScore)
                .HasPrecision(5, 2);
        }
    }
}