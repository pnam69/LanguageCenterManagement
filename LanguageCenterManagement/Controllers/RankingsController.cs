using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class RankingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RankingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? classId)
        {
            int? teacherId = null;

            if (User.IsInRole("Teacher"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user?.TeacherId == null)
                    return Forbid();

                teacherId = user.TeacherId.Value;
            }

            var classQuery = _context.Classes
                .AsNoTracking()
                .AsQueryable();

            if (teacherId.HasValue)
            {
                classQuery = classQuery.Where(c =>
                    c.TeacherId == teacherId.Value);
            }

            var availableClasses = await classQuery
                .OrderBy(c => c.ClassName)
                .Select(c => new RankingClassOptionViewModel
                {
                    ClassId = c.LanguageClassId,
                    ClassName = c.ClassCode + " - " + c.ClassName
                })
                .ToListAsync();

            // A teacher cannot request another teacher's class
            // by manually changing the query string.
            if (classId.HasValue &&
                !availableClasses.Any(c => c.ClassId == classId.Value))
            {
                return Forbid();
            }

            var resultQuery = _context.ExamResults
                .AsNoTracking()
                .Where(r =>
                    r.Status == "Completed" &&
                    r.Exam != null &&
                    r.Exam.MaxScore > 0 &&
                    r.Exam.Status != "Draft");

            if (teacherId.HasValue)
            {
                resultQuery = resultQuery.Where(r =>
                    r.Exam!.Class != null &&
                    r.Exam.Class.TeacherId == teacherId.Value);
            }

            var rawResults = await resultQuery
                .Select(r => new
                {
                    r.StudentId,
                    StudentCode = r.Student != null
                        ? r.Student.StudentCode
                        : "",
                    StudentName = r.Student != null
                        ? r.Student.FullName
                        : "Unknown student",
                    r.ExamId,
                    ClassId = r.Exam!.ClassId,
                    ClassCode = r.Exam.Class != null
                        ? r.Exam.Class.ClassCode
                        : "",
                    ClassName = r.Exam.Class != null
                        ? r.Exam.Class.ClassName
                        : "Unknown class",
                    CourseName = r.Exam.Class != null &&
                                 r.Exam.Class.Course != null
                        ? r.Exam.Class.Course.CourseName
                        : "Unknown course",
                    TeacherId = r.Exam.Class != null
                        ? r.Exam.Class.TeacherId
                        : 0,
                    TeacherCode = r.Exam.Class != null &&
                                  r.Exam.Class.Teacher != null
                        ? r.Exam.Class.Teacher.TeacherCode
                        : "",
                    TeacherName = r.Exam.Class != null &&
                                  r.Exam.Class.Teacher != null
                        ? r.Exam.Class.Teacher.FullName
                        : "Unknown teacher",
                    Percentage = Math.Clamp(
                        r.Score / r.Exam!.MaxScore * 100m,
                        0m,
                        100m)
                })
                .ToListAsync();

            var model = new RankingsViewModel
            {
                SelectedClassId = classId,
                AvailableClasses = availableClasses
            };

            // Student rankings within the selected class.
            if (classId.HasValue)
            {
                model.Students = rawResults
                    .Where(r => r.ClassId == classId.Value)
                    .GroupBy(r => new
                    {
                        r.StudentId,
                        r.StudentCode,
                        r.StudentName
                    })
                    .Select(g => new StudentRankingItemViewModel
                    {
                        StudentId = g.Key.StudentId,
                        StudentCode = g.Key.StudentCode,
                        StudentName = g.Key.StudentName,
                        AveragePercentage = Math.Round(
                            g.Average(r => r.Percentage), 1),
                        CompletedExams = g.Count()
                    })
                    .OrderByDescending(s => s.AveragePercentage)
                    .ThenByDescending(s => s.CompletedExams)
                    .ThenBy(s => s.StudentName)
                    .ToList();

                for (int i = 0; i < model.Students.Count; i++)
                    model.Students[i].Rank = i + 1;
            }

            // Class rankings: average each student's average first,
            // then average those student averages. This avoids
            // giving students with more exams disproportionate weight.
            model.Classes = rawResults
                .GroupBy(r => new
                {
                    r.ClassId,
                    r.ClassCode,
                    r.ClassName,
                    r.CourseName,
                    r.TeacherName
                })
                .Select(classGroup =>
                {
                    var studentAverages = classGroup
                        .GroupBy(r => r.StudentId)
                        .Select(studentGroup =>
                            studentGroup.Average(r => r.Percentage))
                        .ToList();

                    return new ClassRankingItemViewModel
                    {
                        ClassId = classGroup.Key.ClassId,
                        ClassCode = classGroup.Key.ClassCode,
                        ClassName = classGroup.Key.ClassName,
                        CourseName = classGroup.Key.CourseName,
                        TeacherName = classGroup.Key.TeacherName,
                        AveragePercentage = Math.Round(
                            studentAverages.Average(), 1),
                        StudentCount = studentAverages.Count,
                        CompletedResults = classGroup.Count()
                    };
                })
                .OrderByDescending(c => c.AveragePercentage)
                .ThenByDescending(c => c.StudentCount)
                .ThenBy(c => c.ClassName)
                .ToList();

            for (int i = 0; i < model.Classes.Count; i++)
                model.Classes[i].Rank = i + 1;

            // Teacher comparisons: first calculate each student's
            // average across the teacher's classes, then average
            // those student averages.
            model.Teachers = rawResults
                .GroupBy(r => new
                {
                    r.TeacherId,
                    r.TeacherCode,
                    r.TeacherName
                })
                .Select(teacherGroup =>
                {
                    var studentAverages = teacherGroup
                        .GroupBy(r => r.StudentId)
                        .Select(g => g.Average(r => r.Percentage))
                        .ToList();

                    return new TeacherRankingItemViewModel
                    {
                        TeacherId = teacherGroup.Key.TeacherId,
                        TeacherCode = teacherGroup.Key.TeacherCode,
                        TeacherName = teacherGroup.Key.TeacherName,
                        AveragePercentage = Math.Round(
                            studentAverages.Average(), 1),
                        StudentCount = studentAverages.Count,
                        CompletedResults = teacherGroup.Count(),
                        ClassCount = teacherGroup
                            .Select(r => r.ClassId)
                            .Distinct()
                            .Count()
                    };
                })
                .OrderByDescending(t => t.AveragePercentage)
                .ThenByDescending(t => t.StudentCount)
                .ThenBy(t => t.TeacherName)
                .ToList();

            for (int i = 0; i < model.Teachers.Count; i++)
                model.Teachers[i].Rank = i + 1;

            return View(model);
        }
    }
}
