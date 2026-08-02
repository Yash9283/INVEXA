using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;
using StockFlow.Helpers;

namespace StockFlow.Controllers;

// Portal for supplier-role logins. Does NOT extend BaseController, so suppliers
// are not bounced away from it. They can only ever see their own purchase orders.
[SessionAuthorize("Supplier")]
public class SupplierPortalController : Controller
{
    private readonly ApplicationDbContext _context;
    public SupplierPortalController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> MyOrders()
    {
        var adminId = HttpContext.Session.GetInt32("AdminId");
        if (adminId is null) return RedirectToAction("Login", "Account");

        var admin = await _context.Admins.FindAsync(adminId.Value);
        if (admin?.SupplierId is null) return RedirectToAction("Login", "Account");

        var supplier = await _context.Suppliers.FindAsync(admin.SupplierId.Value);
        ViewBag.SupplierName = supplier?.SupplierName ?? "Supplier";

        var orders = await _context.PurchaseOrders.AsNoTracking()
            .Where(po => po.SupplierId == admin.SupplierId.Value)
            .Include(po => po.Items).ThenInclude(i => i.Product)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    // Supplier confirms dispatch: moves a "Sent" order to "Shipped" so the company knows it is on the way.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkShipped(int id)
    {
        var adminId = HttpContext.Session.GetInt32("AdminId");
        if (adminId is null) return RedirectToAction("Login", "Account");

        var admin = await _context.Admins.FindAsync(adminId.Value);
        if (admin?.SupplierId is null) return RedirectToAction("Login", "Account");

        var order = await _context.PurchaseOrders
            .FirstOrDefaultAsync(po => po.Id == id && po.SupplierId == admin.SupplierId.Value);
        if (order is null) return NotFound();

        if (order.Status == "Sent")
        {
            order.Status = "Shipped";
            await _context.SaveChangesAsync();

            var supplier = await _context.Suppliers.FindAsync(admin.SupplierId.Value);
            NotificationHelper.Add(_context,
                $"{supplier?.SupplierName ?? "Supplier"} shipped order {order.PurchaseOrderNumber}. Ready to receive.",
                "Purchase Order", "Admin");
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order {order.PurchaseOrderNumber} marked as shipped. The company has been notified.";
        }
        return RedirectToAction(nameof(MyOrders));
    }
}
