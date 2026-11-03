using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace LanguageCenterManagement.ViewComponents
{
    public class MessagingUnreadCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagingUnreadCountViewComponent(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!(User.IsInRole("Admin") ||
                  User.IsInRole("Teacher") ||
                  User.IsInRole("Student")))
            {
                return View(0);
            }

            var claimsPrincipal = HttpContext.User as ClaimsPrincipal;

            if (claimsPrincipal == null)
                return View(0);

            var user = await _userManager.GetUserAsync(claimsPrincipal);

            if (user == null)
                return View(0);

            var query = _context.ConversationMessages
                .Where(m => !m.IsRead && m.SenderUserId != user.Id);

            if (User.IsInRole("Student"))
            {
                if (!user.StudentId.HasValue)
                    return View(0);

                query = query.Where(
                    m => m.Conversation!.StudentId == user.StudentId.Value);
            }
            else if (User.IsInRole("Teacher"))
            {
                query = query.Where(
                    m => m.Conversation!.RecipientUserId == user.Id);
            }
            // Admin can see unread messages across all conversations.

            var count = await query.CountAsync();

            return View(count);
        }
    }
}
