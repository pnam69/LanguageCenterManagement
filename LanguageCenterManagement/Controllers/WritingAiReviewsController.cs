using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class WritingAiReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WritingAiReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.WritingAiReviews
                .AsNoTracking()
                .Include(r => r.ExamAnswer)
                    .ThenInclude(a => a!.Question)
                .Include(r => r.ExamAnswer)
                    .ThenInclude(a => a!.ExamResult)
                        .ThenInclude(er => er!.Exam)
                            .ThenInclude(e => e!.Class)
                .AsQueryable();

            // Teachers can only see reviews for their own classes.
            if (!User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user?.TeacherId == null)
                {
                    return Forbid();
                }

                var teacherId = user.TeacherId.Value;

                query = query.Where(r =>
                    r.ExamAnswer != null &&
                    r.ExamAnswer.ExamResult != null &&
                    r.ExamAnswer.ExamResult.Exam != null &&
                    r.ExamAnswer.ExamResult.Exam.Class != null &&
                    r.ExamAnswer.ExamResult.Exam.Class.TeacherId
                        == teacherId);
            }

            var reviews = await query
                .OrderByDescending(r => r.Status == "Review")
                .ThenByDescending(r => r.CheckedAt)
                .ToListAsync();

            return View(reviews);
        }
    }
}
