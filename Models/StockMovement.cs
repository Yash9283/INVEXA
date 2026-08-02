using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, StringLength(30)] public string MovementType { get; set; } = string.Empty;
    public int QuantityDelta { get; set; }
    public int BalanceAfter { get; set; }
    [StringLength(80)] public string? ReferenceNumber { get; set; }
    [StringLength(300)] public string? Notes { get; set; }
    [Required, StringLength(100)] public string PerformedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
