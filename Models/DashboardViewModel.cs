using System.Collections.Generic;

namespace StockFlow.Models
{
    public class DashboardViewModel
    {
        // ===== Primary money / health KPIs =====
        public decimal TotalRevenue { get; set; }      // total of all invoices
        public decimal TotalStockValue { get; set; }   // Price x Quantity (inventory value)
        public decimal UnpaidAmount { get; set; }       // total of unpaid invoices
        public decimal SalesRevenue { get; set; }        // total of all sales

        // ===== Count KPIs =====
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalSales { get; set; }
        public int TotalInvoices { get; set; }
        public int LowStockCount { get; set; }           // qty <= reorderLevel
        public int OutOfStockCount { get; set; }         // qty == 0
        public int CriticalStockCount { get; set; }      // qty > 0 but <= 25% of reorderLevel
        public int WarningStockCount { get; set; }       // qty > 25% but <= reorderLevel
        public int HealthyStockCount { get; set; }       // qty > reorderLevel

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
