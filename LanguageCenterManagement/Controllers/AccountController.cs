using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LanguageCenterManagement.Services.Email;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace LanguageCenterManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAppEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IAppEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
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



        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new AccountProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "User",
                LinkedName = user.UserName ?? "User",
                LinkedCode = string.Empty,
                LinkedType = string.Empty
            };

            if (user.Student != null)
            {
                model.LinkedType = "Student";
                model.LinkedName = user.Student.FullName;
                model.LinkedCode = user.Student.StudentCode;
            }
            else if (user.Teacher != null)
            {
                model.LinkedType = "Teacher";
                model.LinkedName = user.Teacher.FullName;
                model.LinkedCode = user.Teacher.TeacherCode;
            }

            return View(model);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            return View(new EditAccountProfileViewModel
            {
                PhoneNumber = user.PhoneNumber
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            EditAccountProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
                return Challenge();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            user.PhoneNumber = model.PhoneNumber?.Trim();

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            if (user.Student != null)
            {
                user.Student.Email = user.Email;
                user.Student.Phone = user.PhoneNumber;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Profile));
        }


        [Authorize]
        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }


        [Authorize]
        [HttpGet]
        public IActionResult LogoutConfirmation()
        {
            return View("Logout");
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

        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(
                model.Email.Trim());

            // Use the same response whether the account exists or not.
            if (user == null)
            {
                _logger.LogWarning("Password recovery: account not found.");
            }
            else if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                _logger.LogWarning("Password recovery: email is not confirmed.");
            }
            else
            {
                _logger.LogInformation("Password recovery: attempting to send email.");

                try
                {
                    var token =
                        await _userManager.GeneratePasswordResetTokenAsync(user);

                    var encodedToken = WebEncoders.Base64UrlEncode(
                        Encoding.UTF8.GetBytes(token));

                    var resetUrl = Url.Action(
                        nameof(ResetPassword),
                        "Account",
                        new
                        {
                            email = user.Email,
                            token = encodedToken
                        },
                        Request.Scheme);

                    if (resetUrl == null)
                        throw new InvalidOperationException(
                            "Could not generate the password reset URL.");

                    var safeUrl = System.Net.WebUtility.HtmlEncode(resetUrl);



                    var html = $"""
                <h2>Password Reset</h2>
                <p>We received a request to reset your
                Language Center account password.</p>
                <p>
                    <a href="{safeUrl}">Reset My Password</a>
                </p>
                <p>If you did not request this, you can ignore
                this email.</p>
                <p>For security, do not share this link with anyone.</p>
                """;

                    await _emailSender.SendAsync(
                        user.Email!,
                        "Reset Your Language Center Password",
                        html);

                }

                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send a password reset email.");

                    TempData["RecoveryError"] =
                        "We couldn't send the recovery email right now. " +
                        "Please try again later.";

                    return RedirectToAction(nameof(ForgotPassword));
                }
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(
            string? email,
            string? token)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string token;

            try
            {
                token = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(model.Token));
            }
            catch (FormatException)
            {
                ModelState.AddModelError(
                    "",
                    "The reset link is invalid or expired.");

                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(
                model.Email.Trim());

            _logger.LogInformation(
                "Password recovery requested. Account found: {Found}, Email confirmed: {Confirmed}",
                user != null,
                user != null && await _userManager.IsEmailConfirmedAsync(user));

            // Use the same response whether the account exists or not.
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))

                if (user == null)
                {
                    ModelState.AddModelError(
                        "",
                        "The reset link is invalid or expired.");

                    return View(model);
                }

            var result = await _userManager.ResetPasswordAsync(
                user,
                token,
                model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }

            return RedirectToAction(
                nameof(ResetPasswordConfirmation));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

    }
}