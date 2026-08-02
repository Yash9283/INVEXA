using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models;

public class Supplier
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string SupplierName { get; set; } = string.Empty;
    [EmailAddress, StringLength(160)] public string? Email { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
