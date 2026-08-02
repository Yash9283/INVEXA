using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models;

public class SaleCreateViewModel
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Required, StringLength(160)] public string CustomerName { get; set; } = string.Empty;
    [StringLength(30)] public string? CustomerPhone { get; set; }
    [EmailAddress, StringLength(160)] public string? CustomerEmail { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
    [Required, StringLength(20)] public string PaymentStatus { get; set; } = "Paid";
}

public class StockAdjustmentViewModel
{
    public int ProductId { get; set; }
    [Range(-999999, 999999)] public int QuantityDelta { get; set; }
    [Required, StringLength(30)] public string MovementType { get; set; } = "Adjustment";
    [StringLength(300)] public string? Notes { get; set; }
}

public class PurchaseOrderCreateViewModel
{
    [Range(1, int.MaxValue)] public int SupplierId { get; set; }
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Range(0, 99999999)] public decimal UnitCost { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}

public class ReorderSuggestion
{
    public Product Product { get; set; } = null!;
    public int AverageDailySales { get; set; }
    public int SuggestedQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReportViewModel
{
    public decimal RevenueLast30Days { get; set; }
    public decimal GrossProfitLast30Days { get; set; }
    public decimal InventoryValue { get; set; }
    public List<Sale> RecentSales { get; set; } = new();
    public List<ReorderSuggestion> ReorderSuggestions { get; set; } = new();
    public List<Product> DeadStockProducts { get; set; } = new();
}
