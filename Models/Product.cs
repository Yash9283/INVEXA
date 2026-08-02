using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockFlow.Models;

public class Product
{
    [Key] public int Id { get; set; }
    [Required] public string ProductName { get; set; } = string.Empty;
    // Keeps the legacy database column while new records use CategoryId.
    [Column("Category")]
    public string? CategoryName { get; set; }
    [Required, StringLength(50)] public string SKU { get; set; } = string.Empty;
    [StringLength(1000)] public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Range(0, 99999999)] public decimal CostPrice { get; set; }
    [Range(0, 99999999)] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int Quantity { get; set; }
    [Range(0, int.MaxValue)] public int ReorderLevel { get; set; } = 10;
    [Range(1, int.MaxValue)] public int ReorderQuantity { get; set; } = 20;
    [Range(0, 365)] public int LeadTimeDays { get; set; } = 3;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    [Timestamp] public byte[]? RowVersion { get; set; }
}
