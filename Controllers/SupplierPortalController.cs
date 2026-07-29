using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;

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
}
