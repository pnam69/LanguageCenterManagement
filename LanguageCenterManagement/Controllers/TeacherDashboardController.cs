using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
