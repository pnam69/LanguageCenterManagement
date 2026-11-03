using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Conversation
    {
        public int ConversationId { get; set; }

        public int? StudentId { get; set; }

        [Required]
        [StringLength(450)]
        public string RecipientUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Student? Student { get; set; }

        public ICollection<ConversationMessage> Messages { get; set; }
            = new List<ConversationMessage>();
    }
}