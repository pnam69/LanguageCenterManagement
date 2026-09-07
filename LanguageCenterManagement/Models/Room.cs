using System.ComponentModel.DataAnnotations;

namespace LanguageCenterManagement.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        [StringLength(20)]
        public string RoomCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RoomName { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int Capacity { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Available";

        public ICollection<Schedule> Schedules { get; set; }
            = new List<Schedule>();
    }
}