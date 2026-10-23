using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize]
    public class AiChatController : Controller
    {
        private readonly AiChatService _chatService;
        private readonly ILogger<AiChatController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AiChatController(
            AiChatService chatService,
            ILogger<AiChatController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _chatService = chatService;
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new AiChatViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            [FromBody] AiChatViewModel model,
            CancellationToken cancellationToken)
        {
            if (model == null ||
                string.IsNullOrWhiteSpace(model.Message) ||
                model.Message.Length > 2000)
            {
                return BadRequest(new
                {
                    error = "Enter a message of up to 2,000 characters."
                });
            }

            try
            {
                var history = (model.History ?? new())
                    .Where(m =>
                        m.Role is "user" or "assistant" &&
                        !string.IsNullOrWhiteSpace(m.Content))
                    .TakeLast(10)
                    .Select(m => new ChatMessage
                    {
                        Role = m.Role,
                        Content = m.Content.Length > 3000
                            ? m.Content[..3000]
                            : m.Content
                    })
                    .ToList();

                var learningContext = await BuildLearningContextAsync(
                    cancellationToken);

                var answer = await _chatService.ReplyAsync(
                    history,
                    model.Message.Trim(),
                    learningContext,
                    cancellationToken);

                return Json(new { answer });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is InvalidOperationException ||
                ex is System.Text.Json.JsonException ||
                ex is TaskCanceledException)
            {
                _logger.LogError(
                    ex,
                    "AI chatbot request failed for user {UserId}.",
                    User.Identity?.Name);

                return StatusCode(503, new
                {
                    error = "The AI tutor is temporarily unavailable. " +
                            "Check that Ollama is running and try again."
                });
            }
        }

        private async Task<string?> BuildLearningContextAsync(
    CancellationToken cancellationToken)
        {
            // Only students receive personalized performance context.
            if (!User.IsInRole("Student"))
                return null;

            var user = await _userManager.GetUserAsync(User);

            if (user?.StudentId == null)
                return null;

            var studentId = user.StudentId.Value;

            // Load only this student's most recent submitted result records.
            var exams = await _context.ExamResults
                .AsNoTracking()
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.SubmittedAt)
                .Take(10)
                .Select(r => new
                {
                    r.ExamResultId,
                    r.Score,
                    ExamName = r.Exam.ExamName,
                    MaxScore = r.Exam.MaxScore,
                    r.SubmittedAt
                })
                .ToListAsync(cancellationToken);

            if (exams.Count == 0)
            {
                return "No exam results are available for this student yet. " +
                       "Give general English learning advice and do not " +
                       "assume the student's weak skills.";
            }

            var resultIds = exams
                .Select(e => e.ExamResultId)
                .ToList();

            var answers = await _context.ExamAnswers
                .AsNoTracking()
                .Where(a => resultIds.Contains(a.ExamResultId))
                .Select(a => new
                {
                    Skill = a.Question.Skill,
                    a.IsCorrect
                })
                .ToListAsync(cancellationToken);

            var lines = new List<string>
                {
                    "This is the authenticated student's own recent exam data.",
                    "Use only evaluated answers when identifying weak skills.",
                    "Do not treat unanswered or ungraded questions as incorrect.",
                    "",
                    "Recent exams:"
                };

            foreach (var exam in exams.Take(5))
            {
                lines.Add(
                    $"- {exam.ExamName}: {exam.Score}/{exam.MaxScore}");
            }

            var evaluatedAnswers = answers
                .Where(a => a.IsCorrect.HasValue)
                .ToList();

            lines.Add("");
            lines.Add("Performance by skill:");

            if (evaluatedAnswers.Count == 0)
            {
                lines.Add(
                    "There are not enough graded answers to identify weak skills.");
            }
            else
            {
                var skillStats = evaluatedAnswers
                    .GroupBy(a => a.Skill.ToString())
                    .Select(g => new
                    {
                        Skill = g.Key,
                        Total = g.Count(),
                        Correct = g.Count(a => a.IsCorrect == true),
                        Accuracy = 100.0 *
                            g.Count(a => a.IsCorrect == true) / g.Count()
                    })
                    .OrderBy(s => s.Accuracy)
                    .ToList();

                foreach (var skill in skillStats)
                {
                    lines.Add(
                        $"- {skill.Skill}: {skill.Correct}/{skill.Total} " +
                        $"correct ({skill.Accuracy:F0}%).");
                }

                var weakSkills = skillStats
                    .Where(s => s.Total >= 3 && s.Accuracy < 70)
                    .Select(s => s.Skill)
                    .ToList();

                lines.Add("");
                lines.Add(
                    weakSkills.Count > 0
                        ? "Potential areas to improve: " +
                          string.Join(", ", weakSkills)
                        : "No clear weak skill can be identified from " +
                          "these results alone.");
            }

            return string.Join("\n", lines);
        }
    }
}
