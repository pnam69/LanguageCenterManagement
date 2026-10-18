using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public StudentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Students
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.StudentCode.Contains(search) ||
                    s.FullName.Contains(search) ||
                    (s.Phone != null && s.Phone.Contains(search)) ||
                    (s.Email != null && s.Email.Contains(search)));
            }

            var students = await query
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            ViewBag.Search = search;

            return View(students);
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c!.Course)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (await _context.Students
                .AnyAsync(s => s.StudentCode == student.StudentCode))
            {
                ModelState.AddModelError(
                    "StudentCode",
                    "Student code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(student);
            }

            student.CreatedAt = DateTime.Now;

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Edit/5

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student model)
        {
            if (id != model.StudentId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            var email = model.Email?.Trim();
            var phone = model.Phone?.Trim();

            // Find the login account linked to this student.
            var account = await _userManager.Users
                .FirstOrDefaultAsync(u => u.StudentId == id);

            // A linked login account must have an email address.
            if (account != null && string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email is required because this student has a login account.");

                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Update the student record.
                student.StudentCode = model.StudentCode;
                student.FullName = model.FullName;
                student.DateOfBirth = model.DateOfBirth;
                student.Gender = model.Gender;
                student.Status = model.Status;
                student.Email = email;
                student.Phone = phone;
                student.Address = model.Address;

                if (account != null)
                {
                    // Keep the login email and username consistent.
                    var emailResult = await _userManager.SetEmailAsync(
                        account, email!);

                    if (!emailResult.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        foreach (var error in emailResult.Errors)
                            ModelState.AddModelError(nameof(model.Email),
                                error.Description);

                        return View(model);
                    }

                    var usernameResult = await _userManager.SetUserNameAsync(
                        account, email!);

                    if (!usernameResult.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        foreach (var error in usernameResult.Errors)
                            ModelState.AddModelError(nameof(model.Email),
                                error.Description);

                        return View(model);
                    }

                    var phoneResult = await _userManager.SetPhoneNumberAsync(
                        account, phone);

                    if (!phoneResult.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        foreach (var error in phoneResult.Errors)
                            ModelState.AddModelError(nameof(model.Phone),
                                error.Description);

                        return View(model);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Student information updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students
                .FindAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Student deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students
                .Any(e => e.StudentId == id);
        }
    }
}