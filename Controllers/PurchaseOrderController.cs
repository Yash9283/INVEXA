using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;
using StockFlow.Models;
using StockFlow.Services;
using StockFlow.Helpers;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class PurchaseOrderController : BaseController
{
    public PurchaseOrderController(ApplicationDbContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.PurchaseOrders.AsNoTracking().Include(p => p.Supplier).Include(p => p.Items).ThenInclude(i => i.Product)
            .OrderByDescending(p => p.CreatedAt).Take(150).ToListAsync();
        ViewBag.Suggestions = await InventoryInsights.GetReorderSuggestionsAsync(_context);
        return View(orders);
    }

    [SessionAuthorize("Admin")]
    [HttpGet]
    public async Task<IActionResult> Create(int? productId, int? quantity)
    {
        await LoadLookups();
        return View(new PurchaseOrderCreateViewModel { ProductId = productId ?? 0, Quantity = quantity ?? 1 });
    }

    [SessionAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderCreateViewModel input)
    {
        var supplier = await _context.Suppliers.FindAsync(input.SupplierId);
        var product = await _context.Products.FindAsync(input.ProductId);
        if (supplier is null) ModelState.AddModelError(nameof(input.SupplierId), "Choose a supplier.");
        if (product is null || !product.IsActive) ModelState.AddModelError(nameof(input.ProductId), "Choose an active product.");
        if (input.UnitCost < 0) ModelState.AddModelError(nameof(input.UnitCost), "Unit cost cannot be negative.");
        if (!ModelState.IsValid) { await LoadLookups(); return View(input); }

        var po = new PurchaseOrder
        {
            PurchaseOrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}",
            SupplierId = input.SupplierId, Status = "Draft", Notes = input.Notes?.Trim(),
            Items = new List<PurchaseOrderItem> { new() { ProductId = input.ProductId, OrderedQuantity = input.Quantity, UnitCost = input.UnitCost } }
        };
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Purchase order created. Mark it sent, then receive goods to increase stock.";
        return RedirectToAction(nameof(Index));
    }

    [SessionAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSent(int id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order is null) return NotFound();
        if (order.Status == "Draft") { order.Status = "Sent"; await _context.SaveChangesAsync(); TempData["Success"] = "Purchase order marked as sent."; }
        return RedirectToAction(nameof(Index));
    }

    [SessionAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id)
    {
        var order = await _context.PurchaseOrders.Include(p => p.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(p => p.Id == id);
        if (order is null) return NotFound();
        if (order.Status == "Received") { TempData["Error"] = "This purchase order is already received."; return RedirectToAction(nameof(Index)); }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        foreach (var item in order.Items)
        {
            var outstanding = item.OrderedQuantity - item.ReceivedQuantity;
            if (outstanding <= 0 || item.Product is null) continue;
            item.Product.Quantity += outstanding;
            item.Product.CostPrice = item.UnitCost;
            item.Product.UpdatedAt = DateTime.UtcNow;
            item.ReceivedQuantity += outstanding;
            _context.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId, MovementType = "Purchase", QuantityDelta = outstanding, BalanceAfter = item.Product.Quantity,
                ReferenceNumber = order.PurchaseOrderNumber, Notes = $"Goods received from {order.SupplierId}",
                PerformedBy = HttpContext.Session.GetString("Username") ?? "System"
            });
        }
        order.Status = "Received";
        order.ReceivedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        NotificationHelper.Add(_context,
        $"Purchase Order {order.PurchaseOrderNumber} received. Stock updated.",
        "Inventory", "All");
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
        TempData["Success"] = "Goods received and stock ledger updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookups()
    {
        ViewBag.Suppliers = new SelectList(await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync(), "Id", "SupplierName");
        ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync(), "Id", "ProductName");
    }
}
