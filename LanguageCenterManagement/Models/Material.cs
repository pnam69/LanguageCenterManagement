using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Material
    {
        public int MaterialId { get; set; }

        public int ClassId { get; set; }
        public LanguageClass? Class { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? FileUrl { get; set; }

        [StringLength(500)]
        public string? ExternalUrl { get; set; }

        [StringLength(20)]
        public string MaterialType { get; set; } = "Document";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}