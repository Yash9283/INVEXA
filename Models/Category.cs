using System.ComponentModel.DataAnnotations;

namespace StockFlow.Models;

public class Category
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [StringLength(300)] public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
