using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.ViewModels
{
    public class MaterialFormViewModel
    {
        public int MaterialId { get; set; }

        [Required]
        [Display(Name = "Class")]
        public int ClassId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "File URL")]
        public string? FileUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "External URL")]
        public string? ExternalUrl { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Material Type")]
        public string MaterialType { get; set; } = "Document";
    }
}