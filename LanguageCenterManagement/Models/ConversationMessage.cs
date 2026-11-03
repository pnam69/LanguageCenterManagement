using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class ConversationMessage
    {
        public int ConversationMessageId { get; set; }

        public int ConversationId { get; set; }

        [Required]
        [StringLength(450)]
        public string SenderUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public Conversation? Conversation { get; set; }
    }
}