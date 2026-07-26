using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models
{
    public class Admin
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Username { get; set; } = string.Empty;

        // Stores a PBKDF2 hash. Existing plain-text values are upgraded on first successful login.
        [Required, StringLength(512)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Role { get; set; } = "Admin";

        // Security question for account recovery
        [StringLength(200)]
        public string? SecurityQuestion { get; set; }

        [StringLength(200)]
        public string? SecurityAnswer { get; set; }
    }
}
