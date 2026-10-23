using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
            {
                return Forbid();
            }

            var studentId = user.StudentId.Value;
            var now = DateTime.Now;

            // Only exams belonging to classes in which this student
            // is enrolled, excluding cancelled enrollments.
            var enrolledExams = _context.Exams
                .AsNoTracking()
                .Where(e =>
                    e.Class != null &&
                    e.Class.Enrollments.Any(enrollment =>
                        enrollment.StudentId == studentId &&
                        enrollment.Status != "Cancelled"));

            // Upcoming exams must be published and not yet started.
            var upcomingExamsQuery = enrolledExams
                .Where(e =>
                    e.Status == "Published" &&
                    e.ExamDate >= now &&
                    !e.ExamResults.Any(r =>
                        r.StudentId == studentId));

            var upcomingExamCount = await upcomingExamsQuery
                .CountAsync(cancellationToken);

            var upcomingExams = await upcomingExamsQuery
                .OrderBy(e => e.ExamDate)
                .Take(5)
                .Select(e => new UpcomingExamDashboardItem
                {
                    ExamId = e.ExamId,
                    ExamName = e.ExamName,
                    ClassName = e.Class!.ClassName,
                    ClassCode = e.Class.ClassCode,
                    ExamType = e.ExamType,
                    ExamDate = e.ExamDate,
                    Duration = e.Duration,
                    MaxScore = e.MaxScore
                })
                .ToListAsync(cancellationToken);

            // Learning-result statistics for the logged-in student only.
            var learningResultsQuery = _context.LearningResults
                .AsNoTracking()
                .Where(r => r.StudentId == studentId);

            var learningResultCount = await learningResultsQuery
                .CountAsync(cancellationToken);

            decimal? averageLearningScore = null;

            if (learningResultCount > 0)
            {
                averageLearningScore = await learningResultsQuery
                    .AverageAsync(
                        r => r.AverageScore,
                        cancellationToken);
            }

            var recentLearningResults = await learningResultsQuery
                .OrderByDescending(r => r.CompletedDate)
                .ThenByDescending(r => r.LearningResultId)
                .Take(3)
                .Select(r => new StudentLearningResultViewModel
                {
                    LearningResultId = r.LearningResultId,
                    ClassCode = r.Class!.ClassCode,
                    ClassName = r.Class.ClassName,
                    CourseName = r.Class.Course!.CourseName,
                    AverageScore = r.AverageScore,
                    Result = r.Result,
                    TeacherComment = r.TeacherComment,
                    CompletedDate = r.CompletedDate
                })
                .ToListAsync(cancellationToken);

            var viewModel = new StudentDashboardViewModel
            {
                UpcomingExamCount = upcomingExamCount,
                LearningResultCount = learningResultCount,
                AverageLearningScore = averageLearningScore,
                UpcomingExams = upcomingExams,
                RecentLearningResults = recentLearningResults
            };

            return View(viewModel);
        }
    }
}
