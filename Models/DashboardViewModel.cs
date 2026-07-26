using System.Collections.Generic;

namespace StockFlow.Models
{
    public class DashboardViewModel
    {
        // ===== Primary money / health KPIs =====
        public decimal TotalRevenue { get; set; }      // invoices ka total
        public decimal TotalStockValue { get; set; }   // Price x Quantity (inventory ki keemat)
        public decimal UnpaidAmount { get; set; }       // unpaid invoices ka total
        public decimal SalesRevenue { get; set; }        // sales ka total

        // ===== Count KPIs =====
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalSales { get; set; }
        public int TotalInvoices { get; set; }
        public int LowStockCount { get; set; }           // qty < 10
        public int OutOfStockCount { get; set; }         // qty == 0

        // ===== Charts data =====
        public List<string> SalesMonths { get; set; } = new();
        public List<decimal> SalesTotals { get; set; } = new();
        public List<string> CategoryNames { get; set; } = new();
        public List<int> CategoryCounts { get; set; } = new();
        public List<string> TopProductNames { get; set; } = new();   // top selling
        public List<int> TopProductQty { get; set; } = new();
        public int PaidCount { get; set; }
        public int UnpaidCount { get; set; }

        // ===== Tables =====
        public List<Product> LowStockProducts { get; set; } = new();
        public List<Invoice> RecentInvoices { get; set; } = new();
        public List<ReorderSuggestion> ReorderSuggestions { get; set; } = new();

        // ===== Live Stock =====
        public List<Product> LiveStockItems { get; set; } = new();
    }
}
