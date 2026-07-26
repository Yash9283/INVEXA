using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;
using StockFlow.Models;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class SalesController : Controller
{
    private readonly ApplicationDbContext _context;
    public SalesController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var sales = await _context.Sales.AsNoTracking().Include(s => s.Product).Include(s => s.Invoice)
            .OrderByDescending(s => s.SaleDate).Take(250).ToListAsync();
        return View(sales);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadProducts();
        return View(new SaleCreateViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> ProductInfo(int id)
    {
        var product = await _context.Products.AsNoTracking().Where(p => p.Id == id && p.IsActive)
            .Select(p => new { p.ProductName, p.SKU, p.Price, p.Quantity }).FirstOrDefaultAsync();
        return product is null ? NotFound() : Json(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleCreateViewModel input)
    {
        if (input.PaymentStatus is not ("Paid" or "Pending")) ModelState.AddModelError(nameof(input.PaymentStatus), "Choose a valid payment status.");
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == input.ProductId && p.IsActive);
        if (product is null) ModelState.AddModelError(nameof(input.ProductId), "Choose an active product.");
        else if (input.Quantity > product.Quantity) ModelState.AddModelError(nameof(input.Quantity), $"Only {product.Quantity} unit(s) are available.");

        if (!ModelState.IsValid)
        {
            await LoadProducts();
            return View(input);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var sale = new Sale
        {
            ProductId = product!.Id, ProductName = product.ProductName, CustomerName = input.CustomerName.Trim(),
            CustomerPhone = input.CustomerPhone?.Trim(), CustomerEmail = input.CustomerEmail?.Trim(), Quantity = input.Quantity,
            UnitPrice = product.Price, TotalAmount = product.Price * input.Quantity, SaleDate = DateTime.UtcNow, Status = "Completed"
        };
        product.Quantity -= input.Quantity;
        product.UpdatedAt = DateTime.UtcNow;
        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{sale.Id:D5}", SaleId = sale.Id, CustomerName = sale.CustomerName,
            Phone = sale.CustomerPhone, Email = sale.CustomerEmail, ProductName = sale.ProductName, Quantity = sale.Quantity,
            UnitPrice = sale.UnitPrice, TotalAmount = sale.TotalAmount, InvoiceDate = sale.SaleDate, PaymentStatus = input.PaymentStatus
        };
        _context.Invoices.Add(invoice);
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id, MovementType = "Sale", QuantityDelta = -input.Quantity, BalanceAfter = product.Quantity,
            ReferenceNumber = invoice.InvoiceNumber, Notes = $"Sale to {sale.CustomerName}",
            PerformedBy = HttpContext.Session.GetString("Username") ?? "System"
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = $"Sale completed and invoice {invoice.InvoiceNumber} generated.";
        return RedirectToAction("Details", "Invoice", new { id = invoice.Id });
    }

    [SessionAuthorize("Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(int id)
    {
        var sale = await _context.Sales.Include(s => s.Product).Include(s => s.Invoice).FirstOrDefaultAsync(s => s.Id == id);
        if (sale is null) return NotFound();
        if (sale.Status == "Voided") { TempData["Error"] = "This sale is already voided."; return RedirectToAction(nameof(Index)); }
        if (sale.Product is null) { TempData["Error"] = "This legacy sale has no linked product and cannot be auto-voided."; return RedirectToAction(nameof(Index)); }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        sale.Product.Quantity += sale.Quantity;
        sale.Product.UpdatedAt = DateTime.UtcNow;
        sale.Status = "Voided";
        if (sale.Invoice is not null) sale.Invoice.PaymentStatus = "Voided";
        _context.StockMovements.Add(new StockMovement
        {
            ProductId = sale.Product.Id, MovementType = "Return", QuantityDelta = sale.Quantity, BalanceAfter = sale.Product.Quantity,
            ReferenceNumber = sale.Invoice?.InvoiceNumber ?? $"VOID-{sale.Id}", Notes = $"Voided sale #{sale.Id}",
            PerformedBy = HttpContext.Session.GetString("Username") ?? "System"
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Sale voided; stock has been returned through the ledger.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadProducts() => ViewBag.Products = new SelectList(
        await _context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync(), "Id", "ProductName");
}
