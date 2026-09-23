using LanguageCenterManagement.Models;
using System.ComponentModel.DataAnnotations;

public class ReadingPassage
{
    public int ReadingPassageId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Passage { get; set; } = string.Empty;

    public ICollection<Question> Questions { get; set; }
        = new List<Question>();
}