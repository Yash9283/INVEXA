using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required, StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(20)]
        public string ForRole { get; set; } = "All";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
