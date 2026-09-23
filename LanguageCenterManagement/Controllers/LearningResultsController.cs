using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class LearningResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LearningResultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var query = _context.LearningResults
                .Include(r => r.Student)
                .Include(r => r.Class)
                    .ThenInclude(c => c!.Course)
                .AsQueryable();

            if (User.IsInRole("Teacher"))
            {
                if (!user.TeacherId.HasValue)
                {
                    return Forbid();
                }

                query = query.Where(r =>
                    r.Class != null &&
                    r.Class.TeacherId == user.TeacherId.Value);
            }

            var results = await query
                .OrderBy(r => r.Class!.ClassCode)
                .ThenBy(r => r.Student!.FullName)
                .ToListAsync();

            return View(results);
        }

        public async Task<IActionResult> Details(int id)
        {
            var result = await _context.LearningResults
                .Include(r => r.Student)
                .Include(r => r.Class)
                    .ThenInclude(c => c!.Course)
                .Include(r => r.Class)
                    .ThenInclude(c => c!.Teacher)
                .FirstOrDefaultAsync(r => r.LearningResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            return View(result);
        }

        public async Task<IActionResult> Create(int? classId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var model = new LearningResultFormViewModel
            {
                ClassId = classId ?? 0,
                AverageScore = 0,
                Result = "InProgress"
            };

            await LoadDropdowns(model, user);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LearningResultFormViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.LanguageClassId == model.ClassId);

            if (classEntity == null)
            {
                ModelState.AddModelError("ClassId", "Class not found.");
            }
            else if (!await CanAccessClass(classEntity))
            {
                return Forbid();
            }
            else
            {
                var enrolled = classEntity.Enrollments
                    .Any(e =>
                        e.StudentId == model.StudentId &&
                        e.Status != "Cancelled");

                if (!enrolled)
                {
                    ModelState.AddModelError(
                        "StudentId",
                        "The selected student is not enrolled in this class.");
                }

                var duplicate = await _context.LearningResults
                    .AnyAsync(r =>
                        r.ClassId == model.ClassId &&
                        r.StudentId == model.StudentId);

                if (duplicate)
                {
                    ModelState.AddModelError(
                        "",
                        "A learning result already exists for this student in this class.");
                }
            }

            if (model.Result == "Completed" && !model.CompletedDate.HasValue)
            {
                model.CompletedDate = DateTime.Now;
            }

            if (model.Result != "Completed")
            {
                model.CompletedDate = null;
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model, user);
                return View(model);
            }

            var result = new LearningResult
            {
                ClassId = model.ClassId,
                StudentId = model.StudentId,
                AverageScore = model.AverageScore,
                Result = model.Result,
                TeacherComment = model.TeacherComment,
                CompletedDate = model.CompletedDate
            };

            _context.LearningResults.Add(result);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Learning result created successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var result = await _context.LearningResults
                .FirstOrDefaultAsync(r => r.LearningResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            var model = new LearningResultFormViewModel
            {
                LearningResultId = result.LearningResultId,
                ClassId = result.ClassId,
                StudentId = result.StudentId,
                AverageScore = result.AverageScore,
                Result = result.Result,
                TeacherComment = result.TeacherComment,
                CompletedDate = result.CompletedDate
            };

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            await LoadDropdowns(model, user);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            LearningResultFormViewModel model)
        {
            if (id != model.LearningResultId)
            {
                return BadRequest();
            }

            var result = await _context.LearningResults
                .FirstOrDefaultAsync(r => r.LearningResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var classEntity = await _context.Classes
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.LanguageClassId == model.ClassId);

            if (classEntity == null)
            {
                ModelState.AddModelError("ClassId", "Class not found.");
            }
            else if (!await CanAccessClass(classEntity))
            {
                return Forbid();
            }
            else
            {
                var enrolled = classEntity.Enrollments
                    .Any(e =>
                        e.StudentId == model.StudentId &&
                        e.Status != "Cancelled");

                if (!enrolled)
                {
                    ModelState.AddModelError(
                        "StudentId",
                        "The selected student is not enrolled in this class.");
                }

                var duplicate = await _context.LearningResults
                    .AnyAsync(r =>
                        r.LearningResultId != id &&
                        r.ClassId == model.ClassId &&
                        r.StudentId == model.StudentId);

                if (duplicate)
                {
                    ModelState.AddModelError(
                        "",
                        "A learning result already exists for this student in this class.");
                }
            }

            if (model.Result == "Completed" && !model.CompletedDate.HasValue)
            {
                model.CompletedDate = DateTime.Now;
            }

            if (model.Result != "Completed")
            {
                model.CompletedDate = null;
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model, user);
                return View(model);
            }

            result.ClassId = model.ClassId;
            result.StudentId = model.StudentId;
            result.AverageScore = model.AverageScore;
            result.Result = model.Result;
            result.TeacherComment = model.TeacherComment;
            result.CompletedDate = model.CompletedDate;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Learning result updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var result = await _context.LearningResults
                .Include(r => r.Student)
                .Include(r => r.Class)
                .FirstOrDefaultAsync(r => r.LearningResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            return View(result);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _context.LearningResults
                .FirstOrDefaultAsync(r => r.LearningResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            if (!await CanAccessResult(result))
            {
                return Forbid();
            }

            _context.LearningResults.Remove(result);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Learning result deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns(
            LearningResultFormViewModel model,
            ApplicationUser user)
        {
            var classesQuery = _context.Classes
                .Include(c => c.Course)
                .AsQueryable();

            if (User.IsInRole("Teacher"))
            {
                if (!user.TeacherId.HasValue)
                {
                    model.ClassId = 0;
                    model.StudentId = 0;
                    return;
                }

                classesQuery = classesQuery.Where(c =>
                    c.TeacherId == user.TeacherId.Value);
            }

            var classes = await classesQuery
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            ViewBag.Classes = new SelectList(
                classes,
                "LanguageClassId",
                "ClassName",
                model.ClassId);

            var studentsQuery = _context.Students
                .AsQueryable();

            if (model.ClassId > 0)
            {
                studentsQuery = studentsQuery.Where(s =>
                    _context.Enrollments.Any(e =>
                        e.StudentId == s.StudentId &&
                        e.ClassId == model.ClassId &&
                        e.Status != "Cancelled"));
            }

            var students = await studentsQuery
                .OrderBy(s => s.FullName)
                .ToListAsync();

            ViewBag.Students = new SelectList(
                students,
                "StudentId",
                "FullName",
                model.StudentId);
        }

        private async Task<bool> CanAccessResult(LearningResult result)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var user = await _userManager.GetUserAsync(User);

            return User.IsInRole("Teacher") &&
                   user?.TeacherId.HasValue == true &&
                   await _context.Classes.AnyAsync(c =>
                       c.LanguageClassId == result.ClassId &&
                       c.TeacherId == user.TeacherId.Value);
        }

        private async Task<bool> CanAccessClass(LanguageClass classEntity)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var user = await _userManager.GetUserAsync(User);

            return User.IsInRole("Teacher") &&
                   user?.TeacherId.HasValue == true &&
                   classEntity.TeacherId == user.TeacherId.Value;
        }
    }
}