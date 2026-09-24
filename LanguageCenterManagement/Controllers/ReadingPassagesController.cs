//using LanguageCenterManagement.Data;
//using LanguageCenterManagement.Models;
//using LanguageCenterManagement.ViewModels;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace LanguageCenterManagement.Controllers
//{
//    [Authorize(Roles = "Admin,Teacher")]
//    public class ReadingPassagesController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public ReadingPassagesController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var passages = await _context.ReadingPassages
//                .Include(p => p.Questions)
//                .OrderByDescending(p => p.ReadingPassageId)
//                .ToListAsync();

//            return View(passages);
//        }

//        public IActionResult Create()
//        {
//            return View(new ReadingPassageCreateViewModel());
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(
//            ReadingPassageCreateViewModel model)
//        {
//            if (!ModelState.IsValid)
//            {
//                return View(model);
//            }

//            var passage = new ReadingPassage
//            {
//                Title = model.Title,
//                Passage = model.Passage
//            };

//            _context.ReadingPassages.Add(passage);

//            await _context.SaveChangesAsync();

//            TempData["Success"] = "Reading passage created successfully.";

//            return RedirectToAction(nameof(Index));
//        }
//    }
//}