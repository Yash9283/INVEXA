using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Models;

namespace StockFlow.Services;

public static class InventoryInsights
{
    public static async Task<List<ReorderSuggestion>> GetReorderSuggestionsAsync(ApplicationDbContext context)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var sold = await context.Sales.Where(s => s.Status == "Completed" && s.SaleDate >= since && s.ProductId != null)
            .GroupBy(s => s.ProductId!.Value).Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) }).ToListAsync();
        var daily = sold.ToDictionary(s => s.ProductId, s => (int)Math.Ceiling(s.Quantity / 30m));
        var products = await context.Products.Where(p => p.IsActive).Include(p => p.Supplier).OrderBy(p => p.Quantity).ToListAsync();
        return products.Where(p => p.Quantity <= p.ReorderLevel || (daily.GetValueOrDefault(p.Id) * p.LeadTimeDays) >= p.Quantity)
            .Select(p =>
            {
                var average = daily.GetValueOrDefault(p.Id);
                var expected = average * p.LeadTimeDays + p.ReorderLevel;
                return new ReorderSuggestion
                {
                    Product = p, AverageDailySales = average,
                    SuggestedQuantity = Math.Max(p.ReorderQuantity, expected - p.Quantity),
                    Reason = p.Quantity == 0 ? "Out of stock" : p.Quantity <= p.ReorderLevel ? "Below reorder level" : "Lead-time demand may cause a stock-out"
                };
            }).ToList();
    }
}
