using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LanguageCenterManagement.ViewModels
{
    public class ConversationListItemViewModel
    {
        public int ConversationId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public class CreateConversationViewModel
    {
        [Required]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string RecipientUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        public List<SelectListItem> Recipients { get; set; } = new();
    }

    public class ConversationDetailsViewModel
    {
        public int ConversationId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public List<MessageItemViewModel> Messages { get; set; } = new();

        [StringLength(5000, MinimumLength = 1)]
        public string ReplyContent { get; set; } = string.Empty;
    }

    public class MessageItemViewModel
    {
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsOwnMessage { get; set; }
    }
}
