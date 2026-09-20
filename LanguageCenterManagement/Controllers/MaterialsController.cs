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
    public class MaterialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MaterialsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? classId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var query = _context.Materials
                .Include(m => m.Class)
                    .ThenInclude(c => c!.Course)
                .AsQueryable();

            if (User.IsInRole("Teacher"))
            {
                if (!user.TeacherId.HasValue)
                {
                    return Forbid();
                }

                query = query.Where(m =>
                    m.Class != null &&
                    m.Class.TeacherId == user.TeacherId.Value);
            }

            if (classId.HasValue)
            {
                query = query.Where(m => m.ClassId == classId.Value);
            }

            var materials = await query
                .OrderBy(m => m.Class!.ClassCode)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync();

            await LoadClasses(user, classId);

            return View(materials);
        }

        public async Task<IActionResult> Details(int id)
        {
            var material = await _context.Materials
                .Include(m => m.Class)
                    .ThenInclude(c => c!.Course)
                .FirstOrDefaultAsync(m => m.MaterialId == id);

            if (material == null)
            {
                return NotFound();
            }

            if (!await CanAccessMaterial(material))
            {
                return Forbid();
            }

            return View(material);
        }

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var model = new MaterialFormViewModel();

            await LoadClasses(user, null);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaterialFormViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.LanguageClassId == model.ClassId);

            if (classEntity == null)
            {
                ModelState.AddModelError(
                    "ClassId",
                    "The selected class does not exist.");
            }
            else if (!await CanAccessClass(classEntity))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(model.FileUrl) &&
                string.IsNullOrWhiteSpace(model.ExternalUrl))
            {
                ModelState.AddModelError(
                    "",
                    "Please provide either a file URL or an external URL.");
            }

            if (!ModelState.IsValid)
            {
                await LoadClasses(user, model.ClassId);
                return View(model);
            }

            var material = new Material
            {
                ClassId = model.ClassId,
                Title = model.Title,
                Description = model.Description,
                FileUrl = model.FileUrl,
                ExternalUrl = model.ExternalUrl,
                MaterialType = model.MaterialType,
                CreatedAt = DateTime.Now
            };

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Material created successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.MaterialId == id);

            if (material == null)
            {
                return NotFound();
            }

            if (!await CanAccessMaterial(material))
            {
                return Forbid();
            }

            var model = new MaterialFormViewModel
            {
                MaterialId = material.MaterialId,
                ClassId = material.ClassId,
                Title = material.Title,
                Description = material.Description,
                FileUrl = material.FileUrl,
                ExternalUrl = material.ExternalUrl,
                MaterialType = material.MaterialType
            };

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            await LoadClasses(user, model.ClassId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            MaterialFormViewModel model)
        {
            if (id != model.MaterialId)
            {
                return BadRequest();
            }

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.MaterialId == id);

            if (material == null)
            {
                return NotFound();
            }

            if (!await CanAccessMaterial(material))
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Forbid();
            }

            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.LanguageClassId == model.ClassId);

            if (classEntity == null)
            {
                ModelState.AddModelError(
                    "ClassId",
                    "The selected class does not exist.");
            }
            else if (!await CanAccessClass(classEntity))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(model.FileUrl) &&
                string.IsNullOrWhiteSpace(model.ExternalUrl))
            {
                ModelState.AddModelError(
                    "",
                    "Please provide either a file URL or an external URL.");
            }

            if (!ModelState.IsValid)
            {
                await LoadClasses(user, model.ClassId);
                return View(model);
            }

            material.ClassId = model.ClassId;
            material.Title = model.Title;
            material.Description = model.Description;
            material.FileUrl = model.FileUrl;
            material.ExternalUrl = model.ExternalUrl;
            material.MaterialType = model.MaterialType;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Material updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var material = await _context.Materials
                .Include(m => m.Class)
                .FirstOrDefaultAsync(m => m.MaterialId == id);

            if (material == null)
            {
                return NotFound();
            }

            if (!await CanAccessMaterial(material))
            {
                return Forbid();
            }

            return View(material);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.MaterialId == id);

            if (material == null)
            {
                return NotFound();
            }

            if (!await CanAccessMaterial(material))
            {
                return Forbid();
            }

            _context.Materials.Remove(material);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Material deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadClasses(
            ApplicationUser user,
            int? selectedClassId)
        {
            var query = _context.Classes
                .Include(c => c.Course)
                .AsQueryable();

            if (User.IsInRole("Teacher"))
            {
                if (!user.TeacherId.HasValue)
                {
                    ViewBag.Classes = new SelectList(
                        Enumerable.Empty<LanguageClass>(),
                        "LanguageClassId",
                        "ClassName");

                    return;
                }

                query = query.Where(c =>
                    c.TeacherId == user.TeacherId.Value);
            }

            var classes = await query
                .OrderBy(c => c.ClassCode)
                .ToListAsync();

            ViewBag.Classes = new SelectList(
                classes,
                "LanguageClassId",
                "ClassName",
                selectedClassId);
        }

        private async Task<bool> CanAccessMaterial(Material material)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var user = await _userManager.GetUserAsync(User);

            if (!User.IsInRole("Teacher") ||
                user == null ||
                !user?.TeacherId.HasValue == true)
            {
                return false;
            }

            return await _context.Classes.AnyAsync(c =>
                c.LanguageClassId == material.ClassId &&
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