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
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Teacher"))
                    {
                        return RedirectToAction("Index", "TeacherDashboard");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Student"))
                    {
                        return RedirectToAction("Index", "StudentDashboard");
                    }
                }

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    "",
                    "Your account has been locked.");
            }
            else
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");
            }

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCreateAccountDropdownsAsync();

            return View(new AccountCreateViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AccountCreateViewModel model)
        {
            if (model.Role != "Admin" &&
                model.Role != "Teacher" &&
                model.Role != "Student")
            {
                ModelState.AddModelError(
                    "Role",
                    "Invalid role selected.");
            }

            if (model.Role == "Teacher")
            {
                if (!model.TeacherId.HasValue)
                {
                    ModelState.AddModelError(
                        "TeacherId",
                        "Please select a teacher.");
                }
                else
                {
                    var teacher = await _context.Teachers
                        .FirstOrDefaultAsync(t =>
                            t.TeacherId == model.TeacherId.Value);

                    if (teacher == null)
                    {
                        ModelState.AddModelError(
                            "TeacherId",
                            "The selected teacher does not exist.");
                    }
                    else
                    {
                        var linkedAccountExists =
                            await _userManager.Users.AnyAsync(u =>
                                u.TeacherId == model.TeacherId.Value);

                        if (linkedAccountExists)
                        {
                            ModelState.AddModelError(
                                "TeacherId",
                                "This teacher already has an account.");
                        }
                    }
                }
            }

            if (model.Role == "Student")
            {
                if (!model.StudentId.HasValue)
                {
                    ModelState.AddModelError(
                        "StudentId",
                        "Please select a student.");
                }
                else
                {
                    var student = await _context.Students
                        .FirstOrDefaultAsync(s =>
                            s.StudentId == model.StudentId.Value);

                    if (student == null)
                    {
                        ModelState.AddModelError(
                            "StudentId",
                            "The selected student does not exist.");
                    }
                    else
                    {
                        var linkedAccountExists =
                            await _userManager.Users.AnyAsync(u =>
                                u.StudentId == model.StudentId.Value);

                        if (linkedAccountExists)
                        {
                            ModelState.AddModelError(
                                "StudentId",
                                "This student already has an account.");
                        }
                    }
                }
            }

            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateAccountDropdownsAsync(
                    model.TeacherId,
                    model.StudentId);

                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            if (model.Role == "Teacher")
            {
                user.TeacherId = model.TeacherId;
            }
            else if (model.Role == "Student")
            {
                user.StudentId = model.StudentId;
            }

            var createResult = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                await LoadCreateAccountDropdownsAsync(
                    model.TeacherId,
                    model.StudentId);

                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                model.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                await LoadCreateAccountDropdownsAsync(
                    model.TeacherId,
                    model.StudentId);

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"Account for {model.Email} was created successfully.";

            return RedirectToAction(nameof(Create));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        private async Task LoadCreateAccountDropdownsAsync(
            int? selectedTeacherId = null,
            int? selectedStudentId = null)
        {
            var teachers = await _context.Teachers
                .OrderBy(t => t.FullName)
                .ToListAsync();

            var students = await _context.Students
                .OrderBy(s => s.FullName)
                .ToListAsync();

            var teachersWithAccounts =
                await _userManager.Users
                    .Where(u => u.TeacherId != null)
                    .Select(u => u.TeacherId!.Value)
                    .ToListAsync();

            var studentsWithAccounts =
                await _userManager.Users
                    .Where(u => u.StudentId != null)
                    .Select(u => u.StudentId!.Value)
                    .ToListAsync();

            ViewData["TeacherId"] = teachers
                .Where(t =>
                    !teachersWithAccounts.Contains(t.TeacherId) ||
                    t.TeacherId == selectedTeacherId)
                .Select(t => new SelectListItem
                {
                    Value = t.TeacherId.ToString(),
                    Text = $"{t.TeacherCode} - {t.FullName}",
                    Selected = t.TeacherId == selectedTeacherId
                })
                .ToList();

            ViewData["StudentId"] = students
                .Where(s =>
                    !studentsWithAccounts.Contains(s.StudentId) ||
                    s.StudentId == selectedStudentId)
                .Select(s => new SelectListItem
                {
                    Value = s.StudentId.ToString(),
                    Text = $"{s.StudentCode} - {s.FullName}",
                    Selected = s.StudentId == selectedStudentId
                })
                .ToList();
        }
    }
}