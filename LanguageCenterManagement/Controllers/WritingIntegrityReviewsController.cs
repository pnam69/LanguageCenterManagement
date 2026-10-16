using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class WritingIntegrityReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WritingIntegrityService _writingIntegrityService;
        public WritingIntegrityReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            WritingIntegrityService writingIntegrityService)
        {
            _context = context;
            _userManager = userManager;
            _writingIntegrityService = writingIntegrityService;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.WritingIntegrityReviews
                .AsNoTracking()
                .Include(r => r.ExamAnswer)
                    .ThenInclude(a => a!.Question)
                .Include(r => r.ExamAnswer)
                    .ThenInclude(a => a!.ExamResult)
                        .ThenInclude(er => er!.Exam)
                            .ThenInclude(e => e!.Class)
                .Where(r => r.Status != "NoMatch");

            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user?.TeacherId == null)
                    return Forbid();

                int teacherId = user.TeacherId.Value;

                query = query.Where(r =>
                    r.ExamAnswer!.ExamResult!.Exam!.Class != null &&
                    r.ExamAnswer.ExamResult.Exam.Class.TeacherId == teacherId);
            }

            var reviews = await query
                .OrderByDescending(r => r.SimilarityPercent)
                .ThenByDescending(r => r.CheckedAt)
                .ToListAsync();

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckUnreviewed()
        {
            var query = _context.ExamAnswers
                .Include(a => a.Question)
                .Include(a => a.ExamResult)
                    .ThenInclude(er => er!.Exam)
                        .ThenInclude(e => e!.Class)
                .Where(a =>
                    a.Question != null &&
                    a.Question.Skill == "Writing" &&
                    a.TextAnswer != null &&
                    a.TextAnswer.Trim() != "" &&
                    !_context.WritingIntegrityReviews.Any(r =>
                        r.ExamAnswerId == a.ExamAnswerId &&
                        r.Status != "NoMatch"));

            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user?.TeacherId == null)
                    return Forbid();

                int teacherId = user.TeacherId.Value;

                query = query.Where(a =>
                    a.ExamResult!.Exam!.Class != null &&
                    a.ExamResult.Exam.Class.TeacherId == teacherId);
            }

            var answerIds = await query
                .Select(a => a.ExamAnswerId)
                .ToListAsync();

            int checkedCount = 0;

            foreach (var answerId in answerIds)
            {
                try
                {
                    await _writingIntegrityService.CheckAnswerAsync(answerId);
                    checkedCount++;
                }
                catch (Exception)
                {
                    // Continue checking the remaining submissions.
                }
            }

            TempData["SuccessMessage"] =
                $"Checked {checkedCount} previously unreviewed writing submissions.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(
            int id,
            string decision,
            string? reviewNote)
        {
            if (decision != "Cleared" && decision != "Confirmed")
                return BadRequest();

            var review = await _context.WritingIntegrityReviews
                .Include(r => r.ExamAnswer)
                    .ThenInclude(a => a!.ExamResult)
                        .ThenInclude(er => er!.Exam)
                            .ThenInclude(e => e!.Class)
                .FirstOrDefaultAsync(
                    r => r.WritingIntegrityReviewId == id);

            if (review == null)
                return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user?.TeacherId == null ||
                    review.ExamAnswer?.ExamResult?.Exam?.Class?.TeacherId
                        != user.TeacherId.Value)
                {
                    return Forbid();
                }
            }

            review.Status = decision;
            review.ReviewNote = string.IsNullOrWhiteSpace(reviewNote)
                ? null
                : reviewNote.Trim();
            review.ReviewedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Review decision saved.";

            return RedirectToAction(nameof(Index));
        }
    }
}