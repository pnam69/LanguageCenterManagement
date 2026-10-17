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
    public class StudentAssessmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentAssessmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return Forbid();

            int studentId = user.StudentId.Value;

            var results = await _context.ExamResults
                .AsNoTracking()
                .Where(r =>
                    r.StudentId == studentId &&
                    r.Status == "Completed")
                .Include(r => r.Exam)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
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

            // Only objectively graded answers are included for now.
            var gradedAnswers = results
                .SelectMany(r => r.ExamAnswers)
                .Where(a =>
                    a.IsCorrect.HasValue &&
                    a.Question != null &&
                    a.Question.Score > 0 &&
                    new[] { "Reading", "Listening", "Speaking", "Writing" }
                        .Contains(a.Question.Skill))
                .ToList();

            model.GradedAnswers = gradedAnswers.Count;

            decimal totalPossible = gradedAnswers.Sum(
                a => a.Question!.Score);

            decimal totalEarned = gradedAnswers.Sum(
                a => a.Score);

            model.OverallPercentage = totalPossible > 0
                ? Math.Round(totalEarned / totalPossible * 100, 1)
                : 0;

            string[] skills =
            {
                "Reading",
                "Listening",
                "Speaking",
                "Writing"
            };

            foreach (var skill in skills)
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

            return View(model);
        }
    }
}
