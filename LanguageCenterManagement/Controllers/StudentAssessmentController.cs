using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentAssessmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AiStudyRecommendationService _recommendationService;
        private readonly ILogger<StudentAssessmentController> _logger;

        public StudentAssessmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AiStudyRecommendationService recommendationService,
            ILogger<StudentAssessmentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _recommendationService = recommendationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            var model = await BuildAssessmentModelAsync(user.StudentId.Value);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecommendations(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            // Always calculate statistics on the server for the
            // currently authenticated student.
            var assessment = await BuildAssessmentModelAsync(
                user.StudentId.Value);

            if (assessment.GradedAnswers == 0)
            {
                return Json(new AiStudyRecommendationViewModel
                {
                    MainFocus = "Complete an exam with graded questions first.",
                    Recommendations = new List<string>
                    {
                        "Complete an available exam to establish your baseline.",
                        "Review the explanations for any incorrect answers.",
                        "Check your assessment again after more answers have been graded."
                    },
                    StudyPlan = "Complete an exam, review your results, and return here when graded data is available."
                });
            }

            try
            {
                var recommendation =
                    await _recommendationService.GenerateAsync(
                        assessment,
                        cancellationToken);

                return Json(recommendation);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to generate AI study recommendations.");

                return StatusCode(503, new
                {
                    message = "AI recommendations are temporarily unavailable. Check that Ollama is running and try again."
                });
            }
        }


        private async Task<StudentAssessmentViewModel>
            BuildAssessmentModelAsync(int studentId)
        {
            var results = await _context.ExamResults
                .AsNoTracking()
                .Where(r =>
                    r.StudentId == studentId &&
                    r.Status == "Completed")
                .Include(r => r.Exam)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                .AsSplitQuery()
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            var model = new StudentAssessmentViewModel
            {
                CompletedExams = results.Count,

                RecentExams = results
                    .Take(5)
                    .Select(r => new StudentRecentExamViewModel
                    {
                        ExamResultId = r.ExamResultId,
                        ExamName = r.Exam?.ExamName ?? "Unknown exam",
                        Score = r.Score,
                        MaxScore = r.Exam?.MaxScore ?? 10,
                        SubmittedAt = r.SubmittedAt
                    })
                    .ToList()
            };

            // Compare the latest three exams against the previous three.
            // Scores are converted to percentages so different exam
            // maximum scores can be compared fairly.
            var examsWithValidScores = results
                .Where(r => (r.Exam?.MaxScore ?? 10) > 0)
                .Select(r => new
                {
                    Percentage = Math.Clamp(
                        r.Score / (r.Exam?.MaxScore ?? 10) * 100m,
                        0m,
                        100m),
                    r.SubmittedAt
                })
                .ToList();

            if (examsWithValidScores.Count >= 4)
            {
                var recent = examsWithValidScores
                    .Take(3)
                    .ToList();

                var previous = examsWithValidScores
                    .Skip(3)
                    .Take(3)
                    .ToList();

                if (previous.Count > 0)
                {
                    model.RecentAveragePercentage = Math.Round(
                        recent.Average(x => x.Percentage), 1);

                    model.PreviousAveragePercentage = Math.Round(
                        previous.Average(x => x.Percentage), 1);

                    model.ProgressDifference = Math.Round(
                        model.RecentAveragePercentage -
                        model.PreviousAveragePercentage, 1);

                    model.HasProgressComparison = true;
                }
            }

            var validSkills = new[]
            {
        "Reading",
        "Listening",
        "Speaking",
        "Writing"
    };

            // Only use objectively graded answers for skill analysis.
            var gradedAnswers = results
                .SelectMany(r => r.ExamAnswers)
                .Where(a =>
                    a.IsCorrect.HasValue &&
                    a.Question != null &&
                    a.Question.Score > 0 &&
                    validSkills.Contains(a.Question.Skill))
                .ToList();

            model.GradedAnswers = gradedAnswers.Count;

            decimal totalPossible = gradedAnswers.Sum(
                a => a.Question!.Score);

            decimal totalEarned = gradedAnswers.Sum(
                a => a.Score);

            model.OverallPercentage = totalPossible > 0
                ? Math.Round(totalEarned / totalPossible * 100, 1)
                : 0;

            foreach (var skill in validSkills)
            {
                var answers = gradedAnswers
                    .Where(a => a.Question!.Skill == skill)
                    .ToList();

                model.Skills.Add(new StudentSkillAssessmentViewModel
                {
                    Skill = skill,
                    GradedQuestions = answers.Count,
                    EarnedScore = answers.Sum(a => a.Score),
                    PossibleScore = answers.Sum(
                        a => a.Question!.Score)
                });
            }

            // Identify questions the student has answered incorrectly
            // more than once across completed exams.
            model.RecurringErrors = gradedAnswers
                .Where(a => a.IsCorrect == false && a.Question != null)
                .GroupBy(a => new
                {
                    a.Question!.QuestionId,
                    a.Question.QuestionText,
                    a.Question.Skill
                })
                .Select(group => new StudentRecurringErrorViewModel
                {
                    QuestionId = group.Key.QuestionId,
                    QuestionText = group.Key.QuestionText
                        ?? "Question text unavailable",
                    Skill = group.Key.Skill ?? "Unknown",
                    IncorrectAttempts = group.Count(),
                    TotalGradedAttempts = gradedAnswers.Count(a =>
                        a.Question!.QuestionId == group.Key.QuestionId)
                })
                .Where(error => error.IncorrectAttempts >= 2)
                .OrderByDescending(error => error.IncorrectAttempts)
                .ThenByDescending(error =>
                    (decimal)error.IncorrectAttempts /
                    Math.Max(error.TotalGradedAttempts, 1))
                .Take(5)
                .ToList();

            return model;
        }

    }
}
