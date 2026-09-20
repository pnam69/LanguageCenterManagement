//using LanguageCenterManagement.Data;
//using LanguageCenterManagement.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;

//namespace LanguageCenterManagement.Controllers
//{
//    [Authorize(Roles = "Admin,Teacher")]
//    public class ExamController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;

//        public ExamController(
//            ApplicationDbContext context,
//            UserManager<ApplicationUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        // GET: Exam
//        public async Task<IActionResult> Index()
//        {
//            var examsQuery = _context.Exams
//                .Include(e => e.Class)
//                    .ThenInclude(c => c!.Course)
//                .AsQueryable();

//            if (User.IsInRole("Teacher"))
//            {
//                var user = await _userManager.GetUserAsync(User);

//                if (user == null || !user.TeacherId.HasValue)
//                {
//                    return Forbid();
//                }

//                examsQuery = examsQuery.Where(e =>
//                    e.Class != null &&
//                    e.Class.TeacherId == user.TeacherId.Value);
//            }

//            var exams = await examsQuery
//                .OrderByDescending(e => e.ExamDate)
//                .ThenBy(e => e.ExamName)
//                .ToListAsync();

//            return View(exams);
//        }

//        // GET: Exam/Details/5
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var exam = await _context.Exams
//                .Include(e => e.Class)
//                    .ThenInclude(c => c!.Course)
//                .Include(e => e.ExamResults)
//                    .ThenInclude(r => r.Student)
//                .Include(e => e.ExamQuestions)
//                    .ThenInclude(eq => eq.Question)
//                .FirstOrDefaultAsync(e => e.ExamId == id);

//            if (exam == null)
//            {
//                return NotFound();
//            }

//            if (!await CanAccessExam(exam))
//            {
//                return Forbid();
//            }

//            return View(exam);
//        }

//        // GET: Exam/Create
//        [HttpGet]
//        public async Task<IActionResult> Create()
//        {
//            await LoadClassDropdown();

//            return View(new Exam
//            {
//                ExamDate = DateTime.Today,
//                MaxScore = 10,
//                Status = "Draft",
//                ExamType = "Test"
//            });
//        }

//        // POST: Exam/Create
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(Exam exam)
//        {
//            if (!await CanManageClass(exam.ClassId))
//            {
//                return Forbid();
//            }

//            if (exam.Duration <= 0)
//            {
//                ModelState.AddModelError(
//                    "Duration",
//                    "Duration must be greater than 0 minutes.");
//            }

//            if (exam.MaxScore <= 0)
//            {
//                ModelState.AddModelError(
//                    "MaxScore",
//                    "Maximum score must be greater than 0.");
//            }

//            if (!ModelState.IsValid)
//            {
//                await LoadClassDropdown(exam.ClassId);
//                return View(exam);
//            }

//            _context.Exams.Add(exam);
//            await _context.SaveChangesAsync();

//            TempData["Success"] = "Exam created successfully.";

//            return RedirectToAction(nameof(Index));
//        }

//        // GET: Exam/Edit/5
//        [HttpGet]
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var exam = await _context.Exams
//                .FirstOrDefaultAsync(e => e.ExamId == id);

//            if (exam == null)
//            {
//                return NotFound();
//            }

//            if (!await CanManageClass(exam.ClassId))
//            {
//                return Forbid();
//            }

//            await LoadClassDropdown(exam.ClassId);

//            return View(exam);
//        }

//        // POST: Exam/Edit/5
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(
//            int id,
//            Exam exam)
//        {
//            if (id != exam.ExamId)
//            {
//                return NotFound();
//            }

//            if (!await CanManageClass(exam.ClassId))
//            {
//                return Forbid();
//            }

//            if (exam.Duration <= 0)
//            {
//                ModelState.AddModelError(
//                    "Duration",
//                    "Duration must be greater than 0 minutes.");
//            }

//            if (exam.MaxScore <= 0)
//            {
//                ModelState.AddModelError(
//                    "MaxScore",
//                    "Maximum score must be greater than 0.");
//            }

//            if (!ModelState.IsValid)
//            {
//                await LoadClassDropdown(exam.ClassId);
//                return View(exam);
//            }

//            var existingExam = await _context.Exams
//                .FirstOrDefaultAsync(e => e.ExamId == id);

//            if (existingExam == null)
//            {
//                return NotFound();
//            }

//            existingExam.ClassId = exam.ClassId;
//            existingExam.ExamName = exam.ExamName;
//            existingExam.ExamType = exam.ExamType;
//            existingExam.ExamDate = exam.ExamDate;
//            existingExam.Duration = exam.Duration;
//            existingExam.MaxScore = exam.MaxScore;
//            existingExam.Status = exam.Status;
//            existingExam.Description = exam.Description;

//            await _context.SaveChangesAsync();

//            TempData["Success"] = "Exam updated successfully.";

//            return RedirectToAction(nameof(Index));
//        }

//        // GET: Exam/Delete/5
//        [HttpGet]
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var exam = await _context.Exams
//                .Include(e => e.Class)
//                    .ThenInclude(c => c!.Course)
//                .FirstOrDefaultAsync(e => e.ExamId == id);

//            if (exam == null)
//            {
//                return NotFound();
//            }

//            if (!await CanManageClass(exam.ClassId))
//            {
//                return Forbid();
//            }

//            return View(exam);
//        }

//        // POST: Exam/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var exam = await _context.Exams
//                .FirstOrDefaultAsync(e => e.ExamId == id);

//            if (exam == null)
//            {
//                return NotFound();
//            }

//            if (!await CanManageClass(exam.ClassId))
//            {
//                return Forbid();
//            }

//            _context.Exams.Remove(exam);
//            await _context.SaveChangesAsync();

//            TempData["Success"] = "Exam deleted successfully.";

//            return RedirectToAction(nameof(Index));
//        }

//        private async Task<bool> CanAccessExam(Exam exam)
//        {
//            if (User.IsInRole("Admin"))
//            {
//                return true;
//            }

//            if (!User.IsInRole("Teacher"))
//            {
//                return false;
//            }

//            var user = await _userManager.GetUserAsync(User);

//            return user?.TeacherId.HasValue == true &&
//                   exam.Class?.TeacherId == user.TeacherId.Value;
//        }

//        private async Task<bool> CanManageClass(int classId)
//        {
//            if (User.IsInRole("Admin"))
//            {
//                return await _context.Classes
//                    .AnyAsync(c => c.LanguageClassId == classId);
//            }

//            if (!User.IsInRole("Teacher"))
//            {
//                return false;
//            }

//            var user = await _userManager.GetUserAsync(User);

//            if (user == null || !user.TeacherId.HasValue)
//            {
//                return false;
//            }

//            return await _context.Classes
//                .AnyAsync(c =>
//                    c.LanguageClassId == classId &&
//                    c.TeacherId == user.TeacherId.Value);
//        }

//        private async Task LoadClassDropdown(int? selectedClassId = null)
//        {
//            var query = _context.Classes
//                .Include(c => c.Course)
//                .OrderBy(c => c.ClassName)
//                .AsQueryable();

//            if (User.IsInRole("Teacher"))
//            {
//                var user = await _userManager.GetUserAsync(User);

//                if (user?.TeacherId.HasValue == true)
//                {
//                    query = query.Where(c =>
//                        c.TeacherId == user.TeacherId.Value);
//                }
//                else
//                {
//                    query = query.Where(c => false);
//                }
//            }

//            var classes = await query.ToListAsync();

//            ViewData["ClassId"] = classes
//                .Select(c => new SelectListItem
//                {
//                    Value = c.LanguageClassId.ToString(),
//                    Text = $"{c.ClassCode} - {c.ClassName}",
//                    Selected = c.LanguageClassId == selectedClassId
//                })
//                .ToList();
//        }
//    }
//}