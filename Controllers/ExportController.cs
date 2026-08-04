using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StockFlow.Data;
using StockFlow.Filters;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class ExportController : BaseController
{
    public ExportController(ApplicationDbContext context) : base(context) { }

    private static readonly XLColor Hdr = XLColor.FromHtml("#0F3040");

    // ─────────────────────────────────────────────────────────────────
    // PRODUCTS
    // ─────────────────────────────────────────────────────────────────

    public IActionResult ProductsExcel()
    {
        var data = _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderBy(p => p.ProductName)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");
        WriteHeaders(ws, new[] { "Product Name", "SKU", "Category", "Supplier", "Cost Price", "Sell Price", "In Stock", "Reorder Level" });

        for (int i = 0; i < data.Count; i++)
        {
            var cat = data[i].Category?.CategoryName ?? data[i].CategoryName ?? "—";
            var sup = data[i].Supplier?.SupplierName ?? "—";
            ws.Cell(i + 2, 1).Value = data[i].ProductName;
            ws.Cell(i + 2, 2).Value = data[i].SKU;
            ws.Cell(i + 2, 3).Value = cat;
            ws.Cell(i + 2, 4).Value = sup;
            ws.Cell(i + 2, 5).Value = (double)data[i].CostPrice;
            ws.Cell(i + 2, 6).Value = (double)data[i].Price;
            ws.Cell(i + 2, 7).Value = data[i].Quantity;
            ws.Cell(i + 2, 8).Value = data[i].ReorderLevel;
        }

        ws.Columns().AdjustToContents();
        return ExcelResult(wb, $"Products_{Stamp()}.xlsx");
    }

    public IActionResult ProductsPdf()
    {
        var data = _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderBy(p => p.ProductName)
            .ToList();

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Element(e => ReportHeader(e, "Products Report", data.Count));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2);
                            c.RelativeColumn(1.5f); c.RelativeColumn(1); c.RelativeColumn(1);
                        });
                        TableHeader(table, "Product", "SKU", "Category", "Price", "Stock", "Reorder");
                        foreach (var p in data)
                        {
                            var cat = p.Category?.CategoryName ?? p.CategoryName ?? "—";
                            TableRow(table, p.ProductName, p.SKU ?? "—", cat,
                                $"₹{p.Price:N2}", p.Quantity.ToString(), p.ReorderLevel.ToString());
                        }
                    });
                });
            });
        });

        return File(pdf.GeneratePdf(), "application/pdf", $"Products_{Stamp()}.pdf");
    }

    // ─────────────────────────────────────────────────────────────────
    // LIVE STOCK
    // ─────────────────────────────────────────────────────────────────

    public IActionResult StockExcel()
    {
        var data = _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Supplier)
            .OrderBy(p => p.Quantity)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Live Stock");
        WriteHeaders(ws, new[] { "Product", "SKU", "Supplier", "Available", "Reorder Level", "Status", "Last Updated" });

        for (int i = 0; i < data.Count; i++)
        {
            string status = data[i].Quantity == 0 ? "Out of Stock"
                          : data[i].Quantity <= data[i].ReorderLevel ? "Low Stock"
                          : "In Stock";

            ws.Cell(i + 2, 1).Value = data[i].ProductName;
            ws.Cell(i + 2, 2).Value = data[i].SKU;
            ws.Cell(i + 2, 3).Value = data[i].Supplier?.SupplierName ?? "—";
            ws.Cell(i + 2, 4).Value = data[i].Quantity;
            ws.Cell(i + 2, 5).Value = data[i].ReorderLevel;
            ws.Cell(i + 2, 6).Value = status;
            ws.Cell(i + 2, 7).Value = data[i].UpdatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm");

            ws.Cell(i + 2, 6).Style.Fill.BackgroundColor =
                status == "Out of Stock" ? XLColor.FromHtml("#f8d7da") :
                status == "Low Stock" ? XLColor.FromHtml("#fff3cd") :
                                          XLColor.FromHtml("#d1e7dd");
        }

        ws.Columns().AdjustToContents();
        return ExcelResult(wb, $"LiveStock_{Stamp()}.xlsx");
    }

    public IActionResult StockPdf()
    {
        var data = _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Supplier)
            .OrderBy(p => p.Quantity)
            .ToList();

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Element(e => ReportHeader(e, "Live Stock Report", data.Count));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3); c.RelativeColumn(2);
                            c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1.5f);
                        });
                        TableHeader(table, "Product", "SKU", "Available", "Reorder", "Status");
                        foreach (var p in data)
                        {
                            string status = p.Quantity == 0 ? "Out of Stock"
                                          : p.Quantity <= p.ReorderLevel ? "Low Stock"
                                          : "In Stock";
                            var statusColor = p.Quantity == 0 ? Colors.Red.Medium
                                           : p.Quantity <= p.ReorderLevel ? Colors.Orange.Medium
                                           : Colors.Green.Medium;
                            TableRowWithBadge(table, status, statusColor,
                                p.ProductName, p.SKU ?? "—",
                                p.Quantity.ToString(), p.ReorderLevel.ToString());
                        }
                    });
                });
            });
        });

        return File(pdf.GeneratePdf(), "application/pdf", $"LiveStock_{Stamp()}.pdf");
    }

    // ─────────────────────────────────────────────────────────────────
    // SUPPLIERS
    // ─────────────────────────────────────────────────────────────────

    public IActionResult SuppliersExcel()
    {
        var data = _context.Suppliers.OrderBy(s => s.SupplierName).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Suppliers");
        WriteHeaders(ws, new[] { "ID", "Supplier Name", "Email", "Phone", "Address" });

        for (int i = 0; i < data.Count; i++)
        {
            ws.Cell(i + 2, 1).Value = data[i].Id;
            ws.Cell(i + 2, 2).Value = data[i].SupplierName;
            ws.Cell(i + 2, 3).Value = data[i].Email ?? "—";
            ws.Cell(i + 2, 4).Value = data[i].Phone ?? "—";
            ws.Cell(i + 2, 5).Value = data[i].Address ?? "—";
        }

        ws.Columns().AdjustToContents();
        return ExcelResult(wb, $"Suppliers_{Stamp()}.xlsx");
    }

    public IActionResult SuppliersPdf()
    {
        var data = _context.Suppliers.OrderBy(s => s.SupplierName).ToList();

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Element(e => ReportHeader(e, "Suppliers Report", data.Count));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.5f); c.RelativeColumn(2.5f);
                            c.RelativeColumn(1.5f); c.RelativeColumn(3);
                        });
                        TableHeader(table, "Supplier Name", "Email", "Phone", "Address");
                        foreach (var s in data)
                            TableRow(table, s.SupplierName, s.Email ?? "—",
                                s.Phone ?? "—", s.Address ?? "—");
                    });
                });
            });
        });

        return File(pdf.GeneratePdf(), "application/pdf", $"Suppliers_{Stamp()}.pdf");
    }

    // ─────────────────────────────────────────────────────────────────
    // INVOICES
    // ─────────────────────────────────────────────────────────────────

    public IActionResult InvoicesExcel()
    {
        var data = _context.Invoices.OrderByDescending(i => i.InvoiceDate).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Invoices");
        WriteHeaders(ws, new[] { "Invoice No", "Date", "Customer", "Product", "Qty", "Unit Price", "Total", "Status" });

        for (int i = 0; i < data.Count; i++)
        {
            ws.Cell(i + 2, 1).Value = data[i].InvoiceNumber;
            ws.Cell(i + 2, 2).Value = data[i].InvoiceDate.ToLocalTime().ToString("dd-MM-yyyy");
            ws.Cell(i + 2, 3).Value = data[i].CustomerName;
            ws.Cell(i + 2, 4).Value = data[i].ProductName;
            ws.Cell(i + 2, 5).Value = data[i].Quantity;
            ws.Cell(i + 2, 6).Value = (double)data[i].UnitPrice;
            ws.Cell(i + 2, 7).Value = (double)data[i].TotalAmount;
            ws.Cell(i + 2, 8).Value = data[i].PaymentStatus;
        }

        ws.Columns().AdjustToContents();
        return ExcelResult(wb, $"Invoices_{Stamp()}.xlsx");
    }

    public IActionResult InvoicesPdf()
    {
        var data = _context.Invoices.OrderByDescending(i => i.InvoiceDate).ToList();

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Element(e => ReportHeader(e, "Invoice Report", data.Count));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2); c.RelativeColumn(1.5f); c.RelativeColumn(2);
                            c.RelativeColumn(2); c.RelativeColumn(0.8f); c.RelativeColumn(1.5f); c.RelativeColumn(1.2f);
                        });
                        TableHeader(table, "Invoice", "Date", "Customer", "Product", "Qty", "Total", "Status");
                        foreach (var inv in data)
                        {
                            var badgeColor = inv.PaymentStatus == "Paid" ? Colors.Green.Medium : Colors.Orange.Medium;
                            TableRowWithBadge(table, inv.PaymentStatus, badgeColor,
                                inv.InvoiceNumber, inv.InvoiceDate.ToLocalTime().ToString("dd-MM-yyyy"),
                                inv.CustomerName, inv.ProductName ?? "—",
                                inv.Quantity.ToString(), $"₹{inv.TotalAmount:N2}");
                        }
                    });
                });
            });
        });

        return File(pdf.GeneratePdf(), "application/pdf", $"Invoices_{Stamp()}.pdf");
    }

    // Single invoice formatted document
    public IActionResult InvoicePdf(int id)
    {
        var inv = _context.Invoices.Find(id);
        if (inv is null) return NotFound();

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.Content().Column(col =>
                {
                    // Header
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("INVEXA").FontSize(22).Bold()
                                .FontColor(Color.FromHex("#0F3040"));
                            c.Item().Text("Inventory Management").FontSize(10)
                                .FontColor(Color.FromHex("#888888"));
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("INVOICE").FontSize(20).Bold()
                                .FontColor(Color.FromHex("#0F3040"));
                            c.Item().Text(inv.InvoiceNumber).FontSize(11);
                            c.Item().Text($"Date: {inv.InvoiceDate.ToLocalTime():dd MMM yyyy}").FontSize(11);
                            c.Item().Background(inv.PaymentStatus == "Paid"
                                ? Color.FromHex("#d1e7dd") : Color.FromHex("#fff3cd"))
                                .Padding(4).Text(inv.PaymentStatus).FontSize(10)
                                .FontColor(inv.PaymentStatus == "Paid"
                                    ? Color.FromHex("#146c43") : Color.FromHex("#856404"));
                        });
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1)
                        .LineColor(Color.FromHex("#dee2e6"));

                    // Bill to
                    col.Item().Background(Color.FromHex("#f8f9fa")).Padding(12).Column(c =>
                    {
                        c.Item().Text("BILLED TO").FontSize(10).Bold()
                            .FontColor(Color.FromHex("#888888"));
                        c.Item().Text(inv.CustomerName).FontSize(13).Bold();
                        if (!string.IsNullOrEmpty(inv.Phone))
                            c.Item().Text($"Phone: {inv.Phone}").FontSize(11);
                        if (!string.IsNullOrEmpty(inv.Email))
                            c.Item().Text($"Email: {inv.Email}").FontSize(11);
                    });

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(30); c.RelativeColumn(4);
                            c.RelativeColumn(1); c.RelativeColumn(1.5f); c.RelativeColumn(1.5f);
                        });
                        TableHeader(table, "#", "Product", "Qty", "Unit Price", "Total");
                        TableRow(table, "1", inv.ProductName ?? "—",
                            inv.Quantity.ToString(),
                            $"₹{inv.UnitPrice:N2}", $"₹{inv.TotalAmount:N2}");

                        // Total row
                        table.Cell().ColumnSpan(4).AlignRight().Padding(8)
                            .Text("Total Amount").Bold().FontSize(13);
                        table.Cell().Background(Color.FromHex("#eef2ff")).Padding(8)
                            .Text($"₹{inv.TotalAmount:N2}").Bold().FontSize(13);
                    });

                    col.Item().PaddingTop(30).AlignCenter()
                        .Text($"Generated by INVEXA  |  {DateTime.Now.ToLocalTime():dd MMM yyyy, hh:mm tt}")
                        .FontSize(10).FontColor(Color.FromHex("#aaaaaa"));
                });
            });
        });

        return File(pdf.GeneratePdf(), "application/pdf", $"{inv.InvoiceNumber}.pdf");
    }

    // ─────────────────────────────────────────────────────────────────
    // SALES
    // ─────────────────────────────────────────────────────────────────

    public IActionResult SalesExcel()
    {
        var data = _context.Sales.OrderByDescending(s => s.SaleDate).Take(500).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales");
        WriteHeaders(ws, new[] { "Date", "Product", "Customer", "Qty", "Unit Price", "Total", "Status" });

        for (int i = 0; i < data.Count; i++)
        {
            ws.Cell(i + 2, 1).Value = data[i].SaleDate.ToLocalTime().ToString("dd-MM-yyyy HH:mm");
            ws.Cell(i + 2, 2).Value = data[i].ProductName;
            ws.Cell(i + 2, 3).Value = data[i].CustomerName;
            ws.Cell(i + 2, 4).Value = data[i].Quantity;
            ws.Cell(i + 2, 5).Value = (double)data[i].UnitPrice;
            ws.Cell(i + 2, 6).Value = (double)data[i].TotalAmount;
            ws.Cell(i + 2, 7).Value = data[i].Status;

            if (data[i].Status == "Voided")
                ws.Row(i + 2).Style.Font.FontColor = XLColor.Gray;
        }

        ws.Columns().AdjustToContents();
        return ExcelResult(wb, $"Sales_{Stamp()}.xlsx");
    }

    public IActionResult SalesPdf()
    {
        var data = _context.Sales.OrderByDescending(s => s.SaleDate).Take(500).ToList();

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Element(e => ReportHeader(e, "Sales Report", data.Count));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2); c.RelativeColumn(2.5f); c.RelativeColumn(2.5f);
                            c.RelativeColumn(0.8f); c.RelativeColumn(1.5f); c.RelativeColumn(1.2f);
                        });
                        TableHeader(table, "Date", "Product", "Customer", "Qty", "Total", "Status");
                        foreach (var s in data)
                        {
                            var badgeColor = s.Status == "Voided" ? Colors.Grey.Medium : Colors.Green.Medium;
                            TableRowWithBadge(table, s.Status, badgeColor,
                                s.SaleDate.ToLocalTime().ToString("dd-MM-yyyy HH:mm"),
                                s.ProductName, s.CustomerName,
                                s.Quantity.ToString(), $"₹{s.TotalAmount:N2}");
                        }
                    });
                });
            });
        });

        return File(pdf.GeneratePdf(), "application/pdf", $"Sales_{Stamp()}.pdf");
    }

    // ─────────────────────────────────────────────────────────────────
    // PURCHASE ORDERS
    // ─────────────────────────────────────────────────────────────────

    public IActionResult PurchaseOrdersExcel()
    {
        var data = _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Purchase Orders");
        WriteHeaders(ws, new[] { "PO Number", "Supplier", "Product", "Ordered", "Received", "Unit Cost", "Status", "Created", "Received On" });

        int row = 2;
        foreach (var po in data)
        {
            foreach (var item in po.Items)
            {
                ws.Cell(row, 1).Value = po.PurchaseOrderNumber;
                ws.Cell(row, 2).Value = po.Supplier?.SupplierName ?? "—";
                ws.Cell(row, 3).Value = item.Product?.ProductName ?? "—";
                ws.Cell(row, 4).Value = item.OrderedQuantity;
                ws.Cell(row, 5).Value = item.ReceivedQuantity;
                ws.Cell(row, 6).Value = (double)item.UnitCost;
                ws.Cell(row, 7).Value = po.Status;
                ws.Cell(row, 8).Value = po.CreatedAt.ToLocalTime().ToString("dd-MM-yyyy");
                ws.Cell(row, 9).Value = po.ReceivedAt.HasValue
                    ? po.ReceivedAt.Value.ToLocalTime().ToString("dd-MM-yyyy") : "—";
                row++;
            }
        }

        ws.Columns().AdjustToContents();
        return ExcelResult(wb, $"PurchaseOrders_{Stamp()}.xlsx");
    }

    public IActionResult PurchaseOrdersPdf()
    {
        var data = _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.Content().Column(col =>
                {
                    col.Item().Element(e => ReportHeader(e, "Purchase Orders Report", data.Count));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(3);
                            c.RelativeColumn(1.5f); c.RelativeColumn(1.5f);
                        });
                        TableHeader(table, "PO Number", "Supplier", "Items", "Created", "Status");
                        foreach (var po in data)
                        {
                            var items = string.Join(", ", po.Items.Select(i =>
                                $"{i.Product?.ProductName ?? "?"} ×{i.OrderedQuantity}"));
                            var badgeColor = po.Status == "Received" ? Colors.Green.Medium
                                           : po.Status == "Sent" ? Colors.Blue.Medium
                                           : Colors.Grey.Medium;
                            TableRowWithBadge(table, po.Status, badgeColor,
                                po.PurchaseOrderNumber,
                                po.Supplier?.SupplierName ?? "—",
                                items,
                                po.CreatedAt.ToLocalTime().ToString("dd-MM-yyyy"));
                        }
                    });
                });
            });
        });

        return File(pdf.GeneratePdf(), "application/pdf", $"PurchaseOrders_{Stamp()}.pdf");
    }

    // ─────────────────────────────────────────────────────────────────
    // SHARED HELPERS
    // ─────────────────────────────────────────────────────────────────

    private static void ReportHeader(IContainer container, string title, int count)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("INVEXA").FontSize(20).Bold()
                        .FontColor(Color.FromHex("#0F3040"));
                    c.Item().Text("Inventory Management").FontSize(9)
                        .FontColor(Color.FromHex("#888888"));
                });
                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text(title).FontSize(16).Bold()
                        .FontColor(Color.FromHex("#0F3040"));
                    c.Item().Text($"Generated: {DateTime.Now.ToLocalTime():dd MMM yyyy, hh:mm tt}")
                        .FontSize(9).FontColor(Color.FromHex("#666666"));
                    c.Item().Text($"Total records: {count}").FontSize(9)
                        .FontColor(Color.FromHex("#666666"));
                });
            });
            col.Item().PaddingVertical(8).LineHorizontal(1)
                .LineColor(Color.FromHex("#0F3040"));
        });
    }

    private static void TableHeader(TableDescriptor table, params string[] headers)
    {
        table.Header(hdr =>
        {
            foreach (var h in headers)
                hdr.Cell().Background(Color.FromHex("#0F3040")).Padding(8)
                    .Text(h).FontSize(10).Bold().FontColor(Colors.White);
        });
    }

    private static void TableRow(TableDescriptor table, params string[] cells)
    {
        foreach (var cell in cells)
            table.Cell().BorderBottom(0.5f).BorderColor(Color.FromHex("#dee2e6"))
                .Padding(7).Text(cell).FontSize(10);
    }

    // Last cell rendered as a coloured badge, rest as plain text
    private static void TableRowWithBadge(TableDescriptor table, string badge, string badgeColor, params string[] cells)
    {
        foreach (var cell in cells)
            table.Cell().BorderBottom(0.5f).BorderColor(Color.FromHex("#dee2e6"))
                .Padding(7).Text(cell).FontSize(10);
        table.Cell().BorderBottom(0.5f).BorderColor(Color.FromHex("#dee2e6"))
            .Padding(7).Element(e =>
                e.Background(Color.FromHex(GetBadgeBg(badgeColor)))
                    .PaddingHorizontal(6).PaddingVertical(2)
                    .Text(badge).FontSize(9).Bold()
                    .FontColor(Color.FromHex(GetBadgeFg(badgeColor))));
    }

    private static string GetBadgeBg(string questColor)
    {
        if (questColor == Colors.Green.Medium) return "#d1e7dd";
        if (questColor == Colors.Orange.Medium) return "#fff3cd";
        if (questColor == Colors.Red.Medium) return "#f8d7da";
        if (questColor == Colors.Blue.Medium) return "#dbeafe";
        return "#f3f4f6";
    }

    private static string GetBadgeFg(string questColor)
    {
        if (questColor == Colors.Green.Medium) return "#146c43";
        if (questColor == Colors.Orange.Medium) return "#856404";
        if (questColor == Colors.Red.Medium) return "#842029";
        if (questColor == Colors.Blue.Medium) return "#1e40af";
        return "#6b7280";
    }


    private void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        var range = ws.Range(1, 1, 1, headers.Length);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = Hdr;
        range.Style.Font.FontColor = XLColor.White;
    }

    private FileContentResult ExcelResult(XLWorkbook wb, string filename)
    {
        wb.Style.Font.FontName = "Times New Roman";
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            filename);
    }
    private static string Stamp() => DateTime.Now.ToString("yyyyMMdd");
}
