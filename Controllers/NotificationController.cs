using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;

namespace StockFlow.Controllers;
[SessionAuthorize]
public class NotificationController : BaseController
{
    public NotificationController(ApplicationDbContext context) : base(context) { }

    // GET /Notification/Index?category=All
    public async Task<IActionResult> Index(string category = "All")
    {
        var role = HttpContext.Session.GetString("Role");

        var query = _context.Notifications
            .Where(n => n.ForRole == "All" || n.ForRole == role)
            .OrderByDescending(n => n.CreatedAt)
            .AsQueryable();

        if (category != "All")
            query = query.Where(n => n.Category == category);

        ViewBag.ActiveCategory = category;
        ViewBag.UnreadCount = await _context.Notifications
            .CountAsync(n => !n.IsRead && (n.ForRole == "All" || n.ForRole == role));

        return View(await query.ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await _context.Notifications.FindAsync(id);
        if (n is not null) { n.IsRead = true; await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var role = HttpContext.Session.GetString("Role");
        var unread = await _context.Notifications
            .Where(n => !n.IsRead && (n.ForRole == "All" || n.ForRole == role))
            .ToListAsync();
        unread.ForEach(n => n.IsRead = true);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearAll()
    {
        var role = HttpContext.Session.GetString("Role");
        var mine = await _context.Notifications
            .Where(n => n.ForRole == "All" || n.ForRole == role)
            .ToListAsync();
        _context.Notifications.RemoveRange(mine);
        await _context.SaveChangesAsync();
        TempData["Success"] = "All notifications cleared.";
        return RedirectToAction(nameof(Index));
    }
}
