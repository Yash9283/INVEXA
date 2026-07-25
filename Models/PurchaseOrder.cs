using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models;

public class PurchaseOrder
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string PurchaseOrderNumber { get; set; } = string.Empty;
    [Required] public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    [Required, StringLength(30)] public string Status { get; set; } = "Draft";
    [StringLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedAt { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
