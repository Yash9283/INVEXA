using StockFlow.Data;
using StockFlow.Models;

namespace StockFlow.Helpers
{
    public static class NotificationHelper
    {
        // forRole: "All" | "Admin" | "User"
        // Caller must call SaveChanges — this batches with the main operation.
        public static void Add(
            ApplicationDbContext context,
            string message,
            string category,
            string forRole = "All")
        {
            context.Notifications.Add(new Notification
            {
                Message = message,
                Category = category,
                ForRole = forRole,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
