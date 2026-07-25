using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockFlow.Data;
using StockFlow.Filters;
using StockFlow.Models;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    public ProductController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive).Include(p => p.Category).Include(p => p.Supplier)
            .OrderBy(p => p.ProductName).ToListAsync();
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookups();
        return View(new Product { ReorderLevel = 10, ReorderQuantity = 20, LeadTimeDays = 3 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        product.ProductName = product.ProductName?.Trim() ?? string.Empty;
        product.SKU = product.SKU?.Trim().ToUpperInvariant() ?? string.Empty;
        await ValidateProduct(product);
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(product);
        }

        product.CategoryName = await CategoryName(product.CategoryId);
        product.UpdatedAt = DateTime.UtcNow;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        if (product.Quantity > 0)
        {
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id, MovementType = "Opening", QuantityDelta = product.Quantity,
                BalanceAfter = product.Quantity, ReferenceNumber = "OPENING", Notes = "Opening inventory",
                PerformedBy = UserName()
            });
            await _context.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        TempData["Success"] = "Product created and opening stock recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null || !product.IsActive) return NotFound();
        await LoadLookups();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string productName, string sku, decimal costPrice, decimal price,
        int reorderLevel, int reorderQuantity, int leadTimeDays, int? categoryId, int? supplierId)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null || !product.IsActive) return NotFound();

        product.ProductName = productName?.Trim() ?? string.Empty;
        product.SKU = sku?.Trim().ToUpperInvariant() ?? string.Empty;
        product.CostPrice = costPrice;
        product.Price = price;
        product.ReorderLevel = reorderLevel;
        product.ReorderQuantity = reorderQuantity;
        product.LeadTimeDays = leadTimeDays;
        product.CategoryId = categoryId;
        product.SupplierId = supplierId;
        await ValidateProduct(product, id);
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(product);
        }

        product.CategoryName = await CategoryName(product.CategoryId);
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Product details updated. Use Stock Adjustments to change stock.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound() : View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return NotFound();
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Product archived. Sales and stock history remain intact.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateProduct(Product product, int? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(product.ProductName)) ModelState.AddModelError(nameof(product.ProductName), "Product name is required.");
        if (string.IsNullOrWhiteSpace(product.SKU)) ModelState.AddModelError(nameof(product.SKU), "SKU is required.");
        if (product.CostPrice < 0 || product.Price < 0) ModelState.AddModelError(nameof(product.Price), "Prices cannot be negative.");
        if (product.ReorderLevel < 0 || product.ReorderQuantity < 1 || product.LeadTimeDays < 0) ModelState.AddModelError(nameof(product.ReorderLevel), "Enter valid reorder settings.");
        if (product.CategoryId is not null && !await _context.Categories.AnyAsync(c => c.Id == product.CategoryId)) ModelState.AddModelError(nameof(product.CategoryId), "Choose a valid category.");
        if (product.SupplierId is not null && !await _context.Suppliers.AnyAsync(s => s.Id == product.SupplierId)) ModelState.AddModelError(nameof(product.SupplierId), "Choose a valid supplier.");
        if (!string.IsNullOrWhiteSpace(product.SKU) && await _context.Products.AnyAsync(p => p.SKU == product.SKU && p.Id != currentId))
            ModelState.AddModelError(nameof(product.SKU), "This SKU is already in use.");
    }

    private async Task<string?> CategoryName(int? categoryId) => categoryId is null ? null :
        await _context.Categories.Where(c => c.Id == categoryId).Select(c => c.CategoryName).FirstOrDefaultAsync();

    private async Task LoadLookups()
    {
        ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(), "Id", "CategoryName");
        ViewBag.Suppliers = new SelectList(await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync(), "Id", "SupplierName");
    }

    private string UserName() => HttpContext.Session.GetString("Username") ?? "System";
}
