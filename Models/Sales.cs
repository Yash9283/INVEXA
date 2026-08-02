using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models;

public class Sale
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    // Snapshot retained for legally useful sales history and old records.
    [Required, StringLength(160)] public string ProductName { get; set; } = string.Empty;
    [Required, StringLength(160)] public string CustomerName { get; set; } = string.Empty;
    [StringLength(30)] public string? CustomerPhone { get; set; }
    [EmailAddress, StringLength(160)] public string? CustomerEmail { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Range(0, 99999999)] public decimal UnitPrice { get; set; }
    [Range(0, 99999999)] public decimal TotalAmount { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    [Required, StringLength(20)] public string Status { get; set; } = "Completed";
    public Invoice? Invoice { get; set; }
}
