using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Vml.Office;
using LanguageCenterManagement.Data;
using LanguageCenterManagement.Models;
using LanguageCenterManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Collections;
using RBush;
using System.Security.Principal;
using static QuestPDF.Helpers.Colors;

namespace LanguageCenterManagement.Controllers
{
    [Authorize(Roles = "Student,Teacher,Admin")]
    public class MessagingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            IQueryable<Conversation> query = _context.Conversations
                .AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.Messages);

            if (User.IsInRole("Student"))
            {
                if (!user.StudentId.HasValue)
                    return Forbid();

                query = query.Where(c => c.StudentId == user.StudentId.Value);
            }
            else if (User.IsInRole("Teacher"))
            {
                query = query.Where(c =>
                    c.RecipientUserId == user.Id ||
                    c.Messages.Any(m => m.SenderUserId == user.Id));
            }
            // Admin can view every conversation.

            var conversations = await query
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync(cancellationToken);

            var userIds = conversations
                .SelectMany(c => c.Messages.Select(m => m.SenderUserId)
                    .Append(c.RecipientUserId))
                .Distinct()
                .ToList();

            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => u.Email ?? u.UserName ?? "User",
                    cancellationToken);

            var result = conversations.Select(c =>
            {
                var lastMessage = c.Messages
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                return new ConversationListItemViewModel
                {
                    ConversationId = c.ConversationId,
                    Subject = c.Subject,
                    StudentName = c.Student?.FullName
                        ?? (c.StudentId.HasValue
                            ? $"Student #{c.StudentId.Value}"
                            : "Direct conversation"),
                    RecipientName = users.TryGetValue(
                        c.RecipientUserId, out var recipientName)
                            ? recipientName
                            : "Staff member",
                    LastMessage = lastMessage?.Content ?? "No messages yet",
                    UpdatedAt = c.UpdatedAt,
                    UnreadCount = c.Messages.Count(m =>
                        !m.IsRead && m.SenderUserId != user.Id)
                };
            }).ToList();

            return View(result);
        }

        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var model = new CreateConversationViewModel();

            await PopulateRecipientsAsync(model, user, HttpContext.RequestAborted);

            return View(model);
        }


        [Authorize(Roles = "Student,Teacher,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateConversationViewModel model,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var allowedRecipientIds = await GetAllowedRecipientIdsAsync(
                user, cancellationToken);

            if (!allowedRecipientIds.Contains(model.RecipientUserId))
            {
                ModelState.AddModelError(
                    nameof(model.RecipientUserId),
                    "You are not allowed to message this recipient.");
            }

            ApplicationUser? recipient = null;
            int? studentId = null;

            if (ModelState.IsValid)
            {
                recipient = await _userManager.FindByIdAsync(
                    model.RecipientUserId);

                if (recipient == null)
                {
                    ModelState.AddModelError(
                        nameof(model.RecipientUserId),
                        "The selected recipient does not exist.");
                }
                else if (User.IsInRole("Student"))
                {
                    if (!user.StudentId.HasValue)
                        return Forbid();

                    studentId = user.StudentId.Value;
                }
                else if (User.IsInRole("Teacher") ||
                         User.IsInRole("Admin"))
                {
                    if (await _userManager.IsInRoleAsync(recipient, "Student"))
                    {
                        if (!recipient.StudentId.HasValue)
                        {
                            ModelState.AddModelError(
                                nameof(model.RecipientUserId),
                                "The selected student has no linked student record.");
                        }
                        else
                        {
                            studentId = recipient.StudentId.Value;
                        }
                    }
                    else if (await _userManager.IsInRoleAsync(recipient, "Admin") ||
                        await _userManager.IsInRoleAsync(recipient, "Teacher"))
                    {
                        // Direct staff-to-staff conversation.
                        studentId = null;
                    }
                    else
                    {
                        ModelState.AddModelError(
                            nameof(model.RecipientUserId),
                            "The selected recipient is not valid.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateRecipientsAsync(
                    model, user, cancellationToken);

                return View(model);
            }

            var now = DateTime.UtcNow;

            var conversation = await FindExistingConversationAsync(
                studentId,
                recipient!.Id,
                cancellationToken);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    StudentId = studentId,
                    RecipientUserId = recipient.Id,
                    Subject = model.Subject.Trim(),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Conversations.Add(conversation);
            }
            else
            {
                conversation.UpdatedAt = now;
            }

            conversation.Messages.Add(new ConversationMessage
            {
                SenderUserId = user.Id,
                Content = model.Content.Trim(),
                SentAt = now,
                IsRead = false
            });

            await _context.SaveChangesAsync(cancellationToken);

            TempData["Success"] = "Your message has been sent.";

            return RedirectToAction(
                nameof(Details),
                new { id = conversation.ConversationId });
        }


        [HttpGet]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var conversation = await _context.Conversations
                .Include(c => c.Student)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(
                    c => c.ConversationId == id,
                    cancellationToken);

            if (conversation == null)
                return NotFound();

            if (!await CanAccessConversationAsync(conversation, user))
                return Forbid();

            var unreadMessages = conversation.Messages
                .Where(m => !m.IsRead && m.SenderUserId != user.Id)
                .ToList();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            if (unreadMessages.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);

            var senderIds = conversation.Messages
                .Select(m => m.SenderUserId)
                .Append(conversation.RecipientUserId)
                .Distinct()
                .ToList();

            var users = await _userManager.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => u.Email ?? u.UserName ?? "User",
                    cancellationToken);

            var model = new ConversationDetailsViewModel
            {
                ConversationId = conversation.ConversationId,
                Subject = conversation.Subject,
                StudentName = conversation.Student?.FullName
                    ?? (conversation.StudentId.HasValue
                        ? $"Student #{conversation.StudentId.Value}"
                        : "Direct conversation"),
                RecipientName = users.TryGetValue(
                    conversation.RecipientUserId, out var recipientName)
                        ? recipientName
                        : "Staff member",
                Messages = conversation.Messages
                    .OrderBy(m => m.SentAt)
                    .Select(m => new MessageItemViewModel
                    {
                        SenderName = users.TryGetValue(
                            m.SenderUserId, out var senderName)
                                ? senderName
                                : "User",
                        Content = m.Content,
                        SentAt = m.SentAt,
                        IsOwnMessage = m.SenderUserId == user.Id
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(
            int id,
            ConversationDetailsViewModel model,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(
                    c => c.ConversationId == id,
                    cancellationToken);

            if (conversation == null)
                return NotFound();

            if (!await CanAccessConversationAsync(conversation, user))
                return Forbid();

            if (string.IsNullOrWhiteSpace(model.ReplyContent) ||
                model.ReplyContent.Trim().Length > 5000)
            {
                TempData["Error"] =
                    "Enter a message containing between 1 and 5000 characters.";

                return RedirectToAction(nameof(Details), new { id });
            }

            var now = DateTime.UtcNow;

            conversation.Messages.Add(new ConversationMessage
            {
                SenderUserId = user.Id,
                Content = model.ReplyContent.Trim(),
                SentAt = now,
                IsRead = false
            });

            conversation.UpdatedAt = now;

            await _context.SaveChangesAsync(cancellationToken);

            TempData["Success"] = "Reply sent.";

            return RedirectToAction(nameof(Details), new { id });
        }


        private async Task<bool> CanAccessConversationAsync(
            Conversation conversation,
            ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return true;

            if (await _userManager.IsInRoleAsync(user, "Student"))
            {
                return user.StudentId.HasValue &&
                       conversation.StudentId == user.StudentId.Value;
            }

            if (await _userManager.IsInRoleAsync(user, "Teacher"))
            {
                return conversation.RecipientUserId == user.Id ||
                       await _context.ConversationMessages.AnyAsync(m =>
                           m.ConversationId == conversation.ConversationId &&
                           m.SenderUserId == user.Id);
            }

            return false;
        }


        private async Task PopulateRecipientsAsync(
            CreateConversationViewModel model,
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var allowedIds = await GetAllowedRecipientIdsAsync(
                user, cancellationToken);

            var users = await _userManager.Users
                .Where(u => allowedIds.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .ToListAsync(cancellationToken);

            model.Recipients = users
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.Email ?? u.UserName ?? u.Id
                })
                .ToList();
        }

        private async Task<List<string>> GetAllowedRecipientIdsAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var students = await _userManager.GetUsersInRoleAsync("Student");
                var teachers = await _userManager.GetUsersInRoleAsync("Teacher");

                return students
                    .Where(u => u.StudentId.HasValue)
                    .Select(u => u.Id)
                    .Concat(teachers.Select(u => u.Id))
                    .Distinct()
                    .ToList();
            }

            if (await _userManager.IsInRoleAsync(user, "Teacher"))
            {
                var allowedIds = new List<string>();

                // Teachers can message any administrator.
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                allowedIds.AddRange(admins.Select(u => u.Id));

                // Teachers can also message students in their classes.
                if (!user.TeacherId.HasValue)
                    return allowedIds.Distinct().ToList();

                var classIds = await _context.Classes
                    .Where(c => c.TeacherId == user.TeacherId.Value)
                    .Select(c => c.LanguageClassId)
                    .ToListAsync(cancellationToken);

                var studentIds = await _context.Enrollments
                    .Where(e => classIds.Contains(e.ClassId))
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var students = await _userManager.GetUsersInRoleAsync("Student");

                allowedIds.AddRange(
                    students
                        .Where(u => u.StudentId.HasValue &&
                                    studentIds.Contains(u.StudentId.Value))
                        .Select(u => u.Id));

                return allowedIds.Distinct().ToList();
            }

            if (await _userManager.IsInRoleAsync(user, "Student"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var allowedIds = admins.Select(u => u.Id).ToList();

                if (!user.StudentId.HasValue)
                    return allowedIds;

                var classIds = await _context.Enrollments
                    .Where(e => e.StudentId == user.StudentId.Value)
                    .Select(e => e.ClassId)
                    .ToListAsync(cancellationToken);

                var teacherIds = await _context.Classes
                    .Where(c => classIds.Contains(c.LanguageClassId))
                    .Select(c => c.TeacherId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var teachers = await _userManager.GetUsersInRoleAsync("Teacher");

                allowedIds.AddRange(
                    teachers
                        .Where(u => u.TeacherId.HasValue &&
                                    teacherIds.Contains(u.TeacherId.Value))
                        .Select(u => u.Id));

                return allowedIds.Distinct().ToList();
            }

            return new List<string>();
        }

        private async Task<Conversation?> FindExistingConversationAsync(
            int? studentId,
            string recipientUserId,
            CancellationToken cancellationToken)
        {
            IQueryable<Conversation> query = _context.Conversations
                .Include(c => c.Messages)
                .Where(c => c.StudentId == studentId);

            if (studentId.HasValue)
            {
                // Student-related conversations use the student and staff recipient.
                return await query
                    .Where(c => c.RecipientUserId == recipientUserId)
                    .OrderByDescending(c => c.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // Direct staff conversations have no StudentId.
            // Match either direction so both participants reuse the same thread.
            return await query
                .Where(c =>
                    (c.RecipientUserId == recipientUserId) ||
                    c.Messages.Any(m => m.SenderUserId == recipientUserId))
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }


    }
}
