using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models;

public class PurchaseOrderItem
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    [Range(1, int.MaxValue)] public int OrderedQuantity { get; set; }
    [Range(0, int.MaxValue)] public int ReceivedQuantity { get; set; }
    [Range(0, 99999999)] public decimal UnitCost { get; set; }
}
