using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;

namespace StockFlow.Controllers;
// Inherited by all session-protected controllers.
// Populates ViewData["UnreadCount"] for the bell badge on every request.
public class BaseController : Controller
{
    protected readonly ApplicationDbContext _context;

    public BaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var role = HttpContext.Session.GetString("Role");
        var username = HttpContext.Session.GetString("Username");

        // Suppliers may only use their own portal - keep them out of admin pages.
        if (role == "Supplier")
        {
            context.Result = new RedirectToActionResult("MyOrders", "SupplierPortal", null);
            return;
        }

        if (!string.IsNullOrEmpty(username))
        {
            ViewData["UnreadCount"] = await _context.Notifications
                .CountAsync(n => !n.IsRead && (n.ForRole == "All" || n.ForRole == role));
        }

        await next();
    }
}
