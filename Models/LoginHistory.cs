using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockFlow.Models
{
    public class LoginHistory
    {
        public int Id { get; set; }

        [Required]
        public int AdminId { get; set; }

        [ForeignKey("AdminId")]
        public Admin Admin { get; set; } = null!;

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }
    }
}
