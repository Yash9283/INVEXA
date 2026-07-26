using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;
using StockFlow.Models;
using StockFlow.Services;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class HomeController : BaseController
{
    public HomeController(ApplicationDbContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();
        var completedSales = _context.Sales.Where(s => s.Status == "Completed");
        model.TotalRevenue = await completedSales.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
        model.SalesRevenue = model.TotalRevenue;
        model.TotalStockValue = await _context.Products.Where(p => p.IsActive).SumAsync(p => (decimal?)(p.CostPrice * p.Quantity)) ?? 0;
        model.UnpaidAmount = await _context.Invoices.Where(i => i.PaymentStatus == "Pending").SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        model.TotalProducts = await _context.Products.CountAsync(p => p.IsActive);
        model.TotalCategories = await _context.Categories.CountAsync();
        model.TotalSuppliers = await _context.Suppliers.CountAsync();
        model.TotalSales = await completedSales.CountAsync();
        model.TotalInvoices = await _context.Invoices.CountAsync();
        model.LowStockCount = await _context.Products.CountAsync(p => p.IsActive && p.Quantity <= p.ReorderLevel);
        model.OutOfStockCount = await _context.Products.CountAsync(p => p.IsActive && p.Quantity == 0);
        model.LowStockProducts = await _context.Products.Where(p => p.IsActive && p.Quantity <= p.ReorderLevel).Include(p => p.Supplier)
            .OrderBy(p => p.Quantity).ToListAsync();
        model.RecentInvoices = await _context.Invoices.OrderByDescending(i => i.InvoiceDate).Take(6).ToListAsync();
        model.PaidCount = await _context.Invoices.CountAsync(i => i.PaymentStatus == "Paid");
        model.UnpaidCount = await _context.Invoices.CountAsync(i => i.PaymentStatus == "Pending");

        var categoryProducts = await _context.Products.Where(p => p.IsActive).Include(p => p.Category).ToListAsync();
        var groupedCategories = categoryProducts.GroupBy(p => p.Category?.CategoryName ?? p.CategoryName ?? "Uncategorized").OrderByDescending(g => g.Count()).ToList();
        model.CategoryNames = groupedCategories.Select(g => g.Key).ToList();
        model.CategoryCounts = groupedCategories.Select(g => g.Count()).ToList();

        var topProducts = await completedSales.GroupBy(s => s.ProductName).Select(g => new { Name = g.Key, Quantity = g.Sum(s => s.Quantity) })
            .OrderByDescending(g => g.Quantity).Take(5).ToListAsync();
        model.TopProductNames = topProducts.Select(p => p.Name).ToList();
        model.TopProductQty = topProducts.Select(p => p.Quantity).ToList();
        var byMonth = await completedSales.GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month }).Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(s => s.TotalAmount) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month).ToListAsync();
        model.SalesMonths = byMonth.Select(s => $"{s.Month}/{s.Year}").ToList();
        model.SalesTotals = byMonth.Select(s => s.Total).ToList();
        model.ReorderSuggestions = await InventoryInsights.GetReorderSuggestionsAsync(_context);
        model.LiveStockItems = await _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .OrderBy(p => p.Quantity)
            .Take(20)
            .ToListAsync();
        return View(model);
    }

    public IActionResult Privacy() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
