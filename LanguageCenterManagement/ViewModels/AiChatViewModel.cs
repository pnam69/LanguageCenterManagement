using System.ComponentModel.DataAnnotations;
using LanguageCenterManagement.Services.Ai;

namespace LanguageCenterManagement.ViewModels
{
    public class AiChatViewModel
    {
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;

        public List<ChatMessage> History { get; set; } = new();
    }
}
