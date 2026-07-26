using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;
using StockFlow.Models;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class StockController : Controller
{
    private readonly ApplicationDbContext _context;
    public StockController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.AsNoTracking().Where(p => p.IsActive)
            .Include(p => p.Category).Include(p => p.Supplier).OrderBy(p => p.Quantity).ToListAsync();
        return View(products);
    }

    public async Task<IActionResult> Movements()
    {
        var movements = await _context.StockMovements.AsNoTracking().Include(m => m.Product)
            .OrderByDescending(m => m.CreatedAt).Take(250).ToListAsync();
        return View(movements);
    }

    [SessionAuthorize("Admin")]
    [HttpGet]
    public async Task<IActionResult> Adjust(int? productId)
    {
        await LoadProducts();
        return View(new StockAdjustmentViewModel { ProductId = productId ?? 0 });
    }

    [SessionAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockAdjustmentViewModel input)
    {
        if (input.QuantityDelta == 0) ModelState.AddModelError(nameof(input.QuantityDelta), "Adjustment cannot be zero.");
        var allowedTypes = new[] { "Adjustment", "Damage", "Return", "Purchase", "Opening" };
        if (!allowedTypes.Contains(input.MovementType)) ModelState.AddModelError(nameof(input.MovementType), "Choose a valid movement type.");
        var product = await _context.Products.FindAsync(input.ProductId);
        if (product is null || !product.IsActive) ModelState.AddModelError(nameof(input.ProductId), "Choose an active product.");
        else if (product.Quantity + input.QuantityDelta < 0) ModelState.AddModelError(nameof(input.QuantityDelta), "This would make stock negative.");

        if (!ModelState.IsValid)
        {
            await LoadProducts();
            return View(input);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        product!.Quantity += input.QuantityDelta;
        product.UpdatedAt = DateTime.UtcNow;
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id, MovementType = input.MovementType, QuantityDelta = input.QuantityDelta,
            BalanceAfter = product.Quantity, Notes = input.Notes?.Trim(), ReferenceNumber = "ADJ-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
            PerformedBy = HttpContext.Session.GetString("Username") ?? "System"
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Stock adjustment saved to the ledger.";
        return RedirectToAction(nameof(Movements));
    }

    private async Task LoadProducts() => ViewBag.Products = new SelectList(
        await _context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync(), "Id", "ProductName");
}
