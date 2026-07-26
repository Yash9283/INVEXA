using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;

namespace StockFlow.Controllers
{
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

            if (!string.IsNullOrEmpty(username))
            {
                ViewData["UnreadCount"] = await _context.Notifications
                    .CountAsync(n => !n.IsRead && (n.ForRole == "All" || n.ForRole == role));
            }

            await next();
        }
    }
}
