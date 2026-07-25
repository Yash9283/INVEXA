using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;
using StockFlow.Models;
using StockFlow.Services;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;
    public ReportController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var sales = await _context.Sales.AsNoTracking().Include(s => s.Product)
            .Where(s => s.Status == "Completed" && s.SaleDate >= since).OrderByDescending(s => s.SaleDate).ToListAsync();
        var deadStock = await _context.Products.AsNoTracking().Where(p => p.IsActive && p.Quantity > 0 &&
            !_context.Sales.Any(s => s.ProductId == p.Id && s.Status == "Completed" && s.SaleDate >= since)).OrderByDescending(p => p.Quantity).ToListAsync();
        var model = new ReportViewModel
        {
            RevenueLast30Days = sales.Sum(s => s.TotalAmount),
            GrossProfitLast30Days = sales.Sum(s => (s.UnitPrice - (s.Product?.CostPrice ?? 0)) * s.Quantity),
            InventoryValue = await _context.Products.Where(p => p.IsActive).SumAsync(p => (decimal?)(p.CostPrice * p.Quantity)) ?? 0,
            RecentSales = sales.Take(20).ToList(), DeadStockProducts = deadStock,
            ReorderSuggestions = await InventoryInsights.GetReorderSuggestionsAsync(_context)
        };
        return View(model);
    }
}
