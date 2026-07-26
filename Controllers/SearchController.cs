using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;
using StockFlow.Filters;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class SearchController : BaseController
{
    public SearchController(ApplicationDbContext context) : base(context) { }

    // GET /Search/Global?q=term
    // ponytail: .AsEnumerable() loads all rows for space-normalised match.
    // Fine at hundreds of rows; add a full-text index if table grows to 10k+.
    [HttpGet]
    public IActionResult Global(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new { products = Array.Empty<object>(), categories = Array.Empty<object>(), suppliers = Array.Empty<object>() });

        var term = q.Trim().ToLowerInvariant().Replace(" ", "");

        var products = _context.Products
            .AsEnumerable()
            .Where(p => p.IsActive && (
                p.ProductName.ToLowerInvariant().Replace(" ", "").Contains(term) ||
                (p.CategoryName ?? p.Category?.CategoryName ?? "").ToLowerInvariant().Replace(" ", "").Contains(term) ||
                p.SKU.ToLowerInvariant().Replace(" ", "").Contains(term)))
            .Select(p => new
            {
                p.Id,
                p.ProductName,
                Category = p.CategoryName ?? p.Category?.CategoryName ?? "—",
                p.SKU
            })
            .Take(6)
            .ToList();

        var categories = _context.Categories
            .AsEnumerable()
            .Where(c =>
                c.CategoryName.ToLowerInvariant().Replace(" ", "").Contains(term) ||
                (c.Description ?? "").ToLowerInvariant().Replace(" ", "").Contains(term))
            .Select(c => new { c.Id, c.CategoryName })
            .Take(6)
            .ToList();

        var suppliers = _context.Suppliers
            .AsEnumerable()
            .Where(s =>
                s.SupplierName.ToLowerInvariant().Replace(" ", "").Contains(term) ||
                (s.Email ?? "").ToLowerInvariant().Replace(" ", "").Contains(term) ||
                (s.Phone ?? "").ToLowerInvariant().Replace(" ", "").Contains(term))
            .Select(s => new { s.Id, s.SupplierName, s.Email })
            .Take(6)
            .ToList();

        return Json(new { products, categories, suppliers });
    }
}
