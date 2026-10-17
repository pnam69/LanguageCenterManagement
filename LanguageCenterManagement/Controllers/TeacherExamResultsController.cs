using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Services.Ai;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherExamResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AiWritingAssessmentService _writingAssessmentService;
        private readonly ILogger<TeacherExamResultsController> _logger;

        public TeacherExamResultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AiWritingAssessmentService writingAssessmentService,
            ILogger<TeacherExamResultsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _writingAssessmentService = writingAssessmentService;
            _logger = logger;
        }

        // GET: TeacherExamResults
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var exams = await _context.Exams
                .Include(e => e.Class)
                .Where(e =>
                    e.Class != null &&
                    e.Class.TeacherId == user.TeacherId.Value)
                .OrderByDescending(e => e.ExamDate)
                .ToListAsync();

            return View(exams);
        }

        // GET: TeacherExamResults/Results/5
        [HttpGet]
        public async Task<IActionResult> Results(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e =>
                    e.ExamId == id &&
                    e.Class != null &&
                    e.Class.TeacherId == user.TeacherId.Value);

            if (exam == null)
            {
                return NotFound();
            }

            var results = await _context.ExamResults
                .Include(r => r.Student)
                .Where(r => r.ExamId == exam.ExamId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            var model = results
                .Select(r => new TeacherExamResultViewModel
                {
                    ExamResultId = r.ExamResultId,
                    ExamId = exam.ExamId,
                    ExamName = exam.ExamName,

                    StudentId = r.StudentId,

                    StudentCode = r.Student?.StudentCode
                        ?? string.Empty,

                    StudentName = r.Student?.FullName
                        ?? $"Student #{r.StudentId}",

                    Score = r.Score,
                    MaxScore = exam.ExamQuestions
                        .Where(eq => eq.Question != null)
                        .Sum(eq => eq.Question!.Score),
                    Status = r.Status,
                    SubmittedAt = r.SubmittedAt
                })
                .ToList();

            ViewBag.ExamName = exam.ExamName;
            ViewBag.ClassName = exam.Class?.ClassName ?? string.Empty;
            ViewBag.ExamDate = exam.ExamDate;
            ViewBag.MaxScore = exam.MaxScore;

            return View(model);
        }

        // POST: TeacherExamResults/Enter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enter(
            TeacherExamResultsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var exam = await _context.Exams
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e =>
                    e.ExamId == model.ExamId &&
                    e.Class != null &&
                    e.Class.TeacherId == user.TeacherId.Value);

            if (exam == null)
            {
                return NotFound();
            }

            ModelState.Clear();

            var validStudentIds = await _context.Enrollments
                .Where(e =>
                    e.ClassId == exam.ClassId &&
                    e.Status != "Rejected" &&
                    e.Status != "Cancelled")
                .Select(e => e.StudentId)
                .ToListAsync();

            foreach (var student in model.Students)
            {
                if (!validStudentIds.Contains(student.StudentId))
                {
                    return BadRequest();
                }

                if (student.Score < 0 ||
                    student.Score > exam.MaxScore)
                {
                    ModelState.AddModelError(
                        $"Students[{model.Students.IndexOf(student)}].Score",
                        $"Score must be between 0 and {exam.MaxScore}.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.ExamName = exam.ExamName;
                model.ClassName = exam.Class?.ClassName ?? string.Empty;
                model.MaxScore = exam.MaxScore;
                model.ExamDate = exam.ExamDate;

                return View(model);
            }

            var existingResults = await _context.ExamResults
                .Where(r => r.ExamId == exam.ExamId)
                .ToListAsync();

            foreach (var student in model.Students)
            {
                var result = existingResults.FirstOrDefault(r =>
                    r.StudentId == student.StudentId);

                if (result == null)
                {
                    result = new ExamResult
                    {
                        ExamId = exam.ExamId,
                        StudentId = student.StudentId
                    };

                    _context.ExamResults.Add(result);
                }

                result.Score = student.Score;
                result.Status = student.Status;
                result.SubmittedAt =
                    student.SubmittedAt ?? DateTime.Now;
                result.Note = student.Note;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Results for {exam.ExamName} were saved successfully.";

            return RedirectToAction(
                nameof(Enter),
                new { id = exam.ExamId });
        }

        // GET: TeacherExamResults/Grade/5
        [HttpGet]
        public async Task<IActionResult> Grade(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
                return Forbid();

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .Include(r => r.Student)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Answer)
                .FirstOrDefaultAsync(r => r.ExamResultId == id);

            if (result == null || result.Exam?.Class == null)
                return NotFound();

            if (result.Exam.Class.TeacherId != user.TeacherId.Value)
                return Forbid();

            var model = await BuildGradeViewModelAsync(result);

            return View(model);
        }

        // POST: TeacherExamResults/Grade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(
            int id,
            TeacherExamGradeViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
            {
                return Forbid();
            }

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(r => r.ExamResultId == id);

            if (result == null || result.Exam == null)
            {
                return NotFound();
            }

            var exam = result.Exam;

            if (exam.Class == null ||
                exam.Class.TeacherId != user.TeacherId.Value)
            {
                return Forbid();
            }

            if (id != model.ExamResultId)
            {
                return BadRequest();
            }

            var examQuestions = await _context.ExamQuestions
                .Include(eq => eq.Question)
                .Where(eq => eq.ExamId == exam.ExamId)
                .OrderBy(eq => eq.QuestionOrder)
                .ToListAsync();

            foreach (var question in examQuestions)
            {
                if (question.Question == null)
                {
                    continue;
                }

                var requiresManualGrading =
                    question.Question.QuestionType.Equals(
                        "Speaking",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.Question.QuestionType.Equals(
                        "Writing",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.Question.Skill.Equals(
                        "Speaking",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.Question.Skill.Equals(
                        "Writing",
                        StringComparison.OrdinalIgnoreCase);

                if (!requiresManualGrading)
                {
                    continue;
                }

                var submittedQuestion = model.Questions
                    .FirstOrDefault(q =>
                        q.QuestionId == question.QuestionId);

                if (submittedQuestion == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"No grade was submitted for question {question.QuestionOrder}.");

                    continue;
                }

                if (submittedQuestion.Score < 0 ||
                    submittedQuestion.Score > question.Question.Score)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Question {question.QuestionOrder} score must be between 0 and {question.Question.Score}.");
                }
            }

            if (!ModelState.IsValid)
            {
                return await ReloadGradeView(result, model);
            }

            foreach (var question in examQuestions)
            {
                if (question.Question == null)
                {
                    continue;
                }

                var answer = result.ExamAnswers
                    .FirstOrDefault(a =>
                        a.QuestionId == question.QuestionId);

                if (answer == null)
                {
                    continue;
                }

                var requiresManualGrading =
                    question.Question.QuestionType.Equals(
                        "Speaking",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.Question.QuestionType.Equals(
                        "Writing",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.Question.Skill.Equals(
                        "Speaking",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.Question.Skill.Equals(
                        "Writing",
                        StringComparison.OrdinalIgnoreCase);

                if (!requiresManualGrading)
                {
                    continue;
                }

                var submittedQuestion = model.Questions
                    .FirstOrDefault(q =>
                        q.QuestionId == question.QuestionId);

                if (submittedQuestion == null)
                {
                    continue;
                }

                answer.Score = submittedQuestion.Score;

                answer.IsCorrect =
                    submittedQuestion.Score >= question.Question.Score;
            }

            var rawScore = result.ExamAnswers
                .Sum(a => a.Score);

            var totalPossibleScore = examQuestions
                .Where(eq => eq.Question != null)
                .Sum(eq => eq.Question!.Score);

            if (totalPossibleScore > 0)
            {
                result.Score = Math.Round(
                    (rawScore / totalPossibleScore) * exam.MaxScore,
                    2);
            }
            else
            {
                result.Score = 0;
            }

            result.Note = model.Note;

            result.Status = "Completed";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Result for {result.Student?.FullName ?? "student"} was graded successfully.";

            return RedirectToAction(
                nameof(Grade),
                new { id = result.ExamResultId });
        }

        // POST: TeacherExamResults/AssessWriting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssessWriting(
            int id,
            int answerId,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.TeacherId.HasValue)
                return Forbid();

            var result = await _context.ExamResults
                .Include(r => r.Exam)
                    .ThenInclude(e => e!.Class)
                .Include(r => r.Student)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q!.WritingContent)
                .Include(r => r.ExamAnswers)
                    .ThenInclude(a => a.Answer)
                .FirstOrDefaultAsync(
                    r => r.ExamResultId == id,
                    cancellationToken);

            if (result == null || result.Exam?.Class == null)
                return NotFound();

            if (result.Exam.Class.TeacherId != user.TeacherId.Value)
                return Forbid();

            var answer = result.ExamAnswers.FirstOrDefault(
                a => a.ExamAnswerId == answerId);

            if (answer?.Question == null)
                return NotFound();

            var question = answer.Question;

            var isWriting =
                string.Equals(question.Skill, "Writing",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(question.QuestionType, "Writing",
                    StringComparison.OrdinalIgnoreCase);

            if (!isWriting)
                return BadRequest("This submission is not a writing answer.");

            var studentWriting = answer.TextAnswer;
            var taskPrompt = question.WritingContent?.Prompt
                ?? question.QuestionText;

            var model = await BuildGradeViewModelAsync(result);

            var item = model.Questions.FirstOrDefault(
                q => q.ExamAnswerId == answerId);

            if (item == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(studentWriting) ||
                studentWriting.Trim().Length < 10)
            {
                item.AiAssessmentError =
                    "The student's writing is empty or too short to assess.";

                return View("Grade", model);
            }

            try
            {
                item.AiAssessment =
                    await _writingAssessmentService.AssessAsync(
                        taskPrompt,
                        studentWriting,
                        cancellationToken);
            }
            catch (OperationCanceledException)
            {
                item.AiAssessmentError =
                    "The assessment was cancelled or took too long. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AI writing assessment failed for ExamAnswerId {AnswerId}.",
                    answerId);

                item.AiAssessmentError =
                    "The AI assessment failed. Check that Ollama is running and try again.";
            }

            return View("Grade", model);
        }

        private async Task PopulateWritingFlagsAsync(TeacherExamGradeViewModel model)
        {
            var answerIds = model.Questions
                .Where(q => q.ExamAnswerId.HasValue)
                .Select(q => q.ExamAnswerId!.Value)
                .Distinct()
                .ToList();

            if (!answerIds.Any())
                return;

            var aiReviews = await _context.WritingAiReviews
                .AsNoTracking()
                .Where(r => answerIds.Contains(r.ExamAnswerId))
                .OrderByDescending(r => r.CheckedAt)
                .ToListAsync();

            var similarityReviews = await _context.WritingIntegrityReviews
                .AsNoTracking()
                .Where(r =>
                    answerIds.Contains(r.ExamAnswerId) &&
                    r.Status != "NoMatch")
                .OrderBy(r => r.Status == "Pending" ? 0 :
                              r.Status == "Confirmed" ? 1 : 2)
                .ThenByDescending(r => r.SimilarityPercent)
                .ToListAsync();

            foreach (var question in model.Questions)
            {
                if (!question.ExamAnswerId.HasValue)
                    continue;

                var answerId = question.ExamAnswerId.Value;

                var aiReview = aiReviews.FirstOrDefault(
                    r => r.ExamAnswerId == answerId);

                if (aiReview != null)
                {
                    question.AiReviewStatus = aiReview.Status;
                    question.AiReviewFlagged =
                        string.Equals(
                            aiReview.Status,
                            "Review",
                            StringComparison.OrdinalIgnoreCase);

                    question.AiReviewIndicators = aiReview.Indicators;
                    question.AiReviewExplanation = aiReview.Explanation;
                    question.AiReviewCheckedAt = aiReview.CheckedAt;
                }

                var similarityReview = similarityReviews.FirstOrDefault(
                    r => r.ExamAnswerId == answerId);

                if (similarityReview != null)
                {
                    question.SimilarityDetected = true;
                    question.SimilarityPercent =
                        similarityReview.SimilarityPercent;

                    question.SimilarityStatus = similarityReview.Status;
                    question.SimilarityMatchingText =
                        similarityReview.MatchingText;

                    question.SimilarityReviewId =
                        similarityReview.WritingIntegrityReviewId;
                }
            }
        }

        private async Task<TeacherExamGradeViewModel> BuildGradeViewModelAsync(ExamResult result)
        {
            var exam = result.Exam!;

            var examQuestions = await _context.ExamQuestions
                .Include(eq => eq.Question)
                    .ThenInclude(q => q!.Answers)
                .Include(eq => eq.Question)
                    .ThenInclude(q => q!.SpeakingContent)
                .Include(eq => eq.Question)
                    .ThenInclude(q => q!.WritingContent)
                .Where(eq => eq.ExamId == exam.ExamId)
                .OrderBy(eq => eq.QuestionOrder)
                .ToListAsync();

            var model = new TeacherExamGradeViewModel
            {
                ExamResultId = result.ExamResultId,
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                ClassName = exam.Class?.ClassName ?? string.Empty,
                StudentId = result.StudentId,
                StudentCode = result.Student?.StudentCode ?? string.Empty,
                StudentName = result.Student?.FullName
                    ?? $"Student #{result.StudentId}",
                Score = result.Score,
                MaxScore = examQuestions
                    .Where(eq => eq.Question != null)
                    .Sum(eq => eq.Question!.Score),
                Status = result.Status,
                SubmittedAt = result.SubmittedAt,
                Note = result.Note
            };

            foreach (var examQuestion in examQuestions)
            {
                var question = examQuestion.Question;

                if (question == null)
                    continue;

                var answer = result.ExamAnswers.FirstOrDefault(
                    a => a.QuestionId == question.QuestionId);

                var item = new TeacherExamGradeQuestionViewModel
                {
                    ExamAnswerId = answer?.ExamAnswerId,
                    ExamQuestionId = examQuestion.ExamQuestionId,
                    QuestionId = question.QuestionId,
                    QuestionOrder = examQuestion.QuestionOrder,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    Skill = question.Skill,
                    MaxScore = question.Score,
                    Score = answer?.Score ?? 0,
                    IsCorrect = answer?.IsCorrect,
                    AudioAnswerUrl = answer?.AudioAnswerUrl,

                    RequiresManualGrading =
                        string.Equals(question.QuestionType, "Speaking",
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(question.QuestionType, "Writing",
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(question.Skill, "Speaking",
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(question.Skill, "Writing",
                            StringComparison.OrdinalIgnoreCase)
                };

                if (answer?.AnswerId != null)
                {
                    item.StudentAnswer = question.Answers
                        .FirstOrDefault(a => a.AnswerId == answer.AnswerId)
                        ?.AnswerText;
                }
                else
                {
                    item.StudentAnswer = answer?.TextAnswer;
                }

                item.CorrectAnswer = question.Answers
                    .FirstOrDefault(a => a.IsCorrect)?.AnswerText;

                if (question.SpeakingContent != null)
                {
                    item.SpeakingPrompt = question.SpeakingContent.Prompt;
                    item.SpeakingPreparationTime =
                        question.SpeakingContent.PreparationTime;
                    item.SpeakingResponseTime =
                        question.SpeakingContent.ResponseTime;
                }

                if (question.WritingContent != null)
                {
                    item.WritingPrompt = question.WritingContent.Prompt;
                    item.WritingTaskPrompt = question.WritingContent.Prompt;
                }
                else
                {
                    item.WritingTaskPrompt = question.QuestionText;
                }

                model.Questions.Add(item);
            }

            await PopulateWritingFlagsAsync(model);

            return model;
        }

        private async Task<IActionResult> ReloadGradeView(
            ExamResult result,
            TeacherExamGradeViewModel model)
        {
            var exam = result.Exam;

            if (exam == null || exam.Class == null)
            {
                return NotFound();
            }

            var examQuestions = await _context.ExamQuestions
                .Include(eq => eq.Question)
                    .ThenInclude(q => q!.Answers)
                .Include(eq => eq.Question)
                    .ThenInclude(q => q!.SpeakingContent)
                .Include(eq => eq.Question)
                    .ThenInclude(q => q!.WritingContent)
                .Where(eq => eq.ExamId == exam.ExamId)
                .OrderBy(eq => eq.QuestionOrder)
                .ToListAsync();

            foreach (var examQuestion in examQuestions)
            {
                var question = examQuestion.Question;

                if (question == null)
                {
                    continue;
                }

                var existingAnswer = result.ExamAnswers
                    .FirstOrDefault(a =>
                        a.QuestionId == question.QuestionId);

                var submittedQuestion = model.Questions
                    .FirstOrDefault(q =>
                        q.QuestionId == question.QuestionId);

                submittedQuestion.ExamAnswerId =
                    existingAnswer?.ExamAnswerId;

                if (submittedQuestion == null)
                {
                    continue;
                }

                submittedQuestion.ExamQuestionId =
                    examQuestion.ExamQuestionId;

                submittedQuestion.QuestionText =
                    question.QuestionText;

                submittedQuestion.QuestionType =
                    question.QuestionType;

                submittedQuestion.Skill =
                    question.Skill;

                submittedQuestion.MaxScore =
                    question.Score;

                submittedQuestion.IsCorrect =
                    existingAnswer?.IsCorrect;

                submittedQuestion.StudentAnswer =
                    existingAnswer?.TextAnswer;

                submittedQuestion.AudioAnswerUrl =
                    existingAnswer?.AudioAnswerUrl;

                submittedQuestion.RequiresManualGrading =
                    question.QuestionType.Equals(
                        "Speaking",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.QuestionType.Equals(
                        "Writing",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.Skill.Equals(
                        "Speaking",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    question.Skill.Equals(
                        "Writing",
                        StringComparison.OrdinalIgnoreCase);
            }

            await PopulateWritingFlagsAsync(model);
            return View("Grade", model);
        }
    }
}