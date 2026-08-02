using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockFlow.Models;

public class Invoice
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string InvoiceNumber { get; set; } = string.Empty;

    // This maps to the existing CustomerName database column.
    [Required, Column("CustomerName")] public string CustomerName { get; set; } = string.Empty;
    [StringLength(30)] public string? Phone { get; set; }
    [EmailAddress, StringLength(160)] public string? Email { get; set; }
    [Required] public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    [Required, StringLength(20)] public string PaymentStatus { get; set; } = "Paid";
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }
}
