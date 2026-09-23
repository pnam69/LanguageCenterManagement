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
    public class StudentLearningResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentLearningResultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !user.StudentId.HasValue)
            {
                return Forbid();
            }

            var results = await _context.LearningResults
                .Include(r => r.Class)
                    .ThenInclude(c => c!.Course)
                .Where(r => r.StudentId == user.StudentId.Value)
                .OrderByDescending(r => r.CompletedDate)
                .ThenBy(r => r.Class!.ClassCode)
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
                .ToListAsync();

            return View(results);
        }
    }
}