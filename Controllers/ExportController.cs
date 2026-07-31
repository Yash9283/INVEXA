using ClosedXML.Excel;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Data;
using StockFlow.Filters;
using System.Text;

namespace StockFlow.Controllers;

[SessionAuthorize]
public class ExportController : BaseController
{
    private readonly IConverter _pdf;

    public ExportController(ApplicationDbContext context, IConverter pdf) : base(context)
        => _pdf = pdf;

    // ── shared accent colour ──────────────────────────────────────────
    private static readonly XLColor Hdr = XLColor.FromHtml("#0F3040");

    // ─────────────────────────────────────────────────────────────────
    // PRODUCTS
    // ─────────────────────────────────────────────────────────────────

    public IActionResult ProductsExcel()
    {
        // Only active products — INVEXA_new has IsActive flag
        var data = _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderBy(p => p.ProductName)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");
        var heads = new[] { "Product Name", "SKU", "Category", "Supplier", "Cost Price", "Sell Price", "In Stock", "Reorder Level" };
        WriteHeaders(ws, heads);

        for (int i = 0; i < data.Count; i++)
        {
            // CategoryName: check nav property first, fall back to legacy string column
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

        var rows = new StringBuilder();
        foreach (var p in data)
        {
            var cat = p.Category?.CategoryName ?? p.CategoryName ?? "—";
            rows.Append($"<tr><td>{Esc(p.ProductName)}</td><td>{Esc(p.SKU)}</td>" +
                        $"<td>{Esc(cat)}</td><td>₹{p.Price:N2}</td>" +
                        $"<td>{p.Quantity}</td><td>{p.ReorderLevel}</td></tr>");
        }

        return PdfResult(
            BuildPage("Products Report",
                "<tr><th>Product</th><th>SKU</th><th>Category</th><th>Price</th><th>Stock</th><th>Reorder</th></tr>",
                rows.ToString(), $"Total: {data.Count}", false),
            $"Products_{Stamp()}.pdf", false);
    }

    // ─────────────────────────────────────────────────────────────────
    // LIVE STOCK
    // ─────────────────────────────────────────────────────────────────

    public IActionResult StockExcel()
    {
        var data = _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Supplier)
            .OrderBy(p => p.Quantity)   // lowest stock first — most useful sort
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Live Stock");
        WriteHeaders(ws, new[] { "Product", "SKU", "Supplier", "Available", "Reorder Level", "Status", "Last Updated" });

        for (int i = 0; i < data.Count; i++)
        {
            // Status matches the view badge logic
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

        var rows = new StringBuilder();
        foreach (var p in data)
        {
            string status = p.Quantity == 0 ? "Out of Stock"
                          : p.Quantity <= p.ReorderLevel ? "Low Stock"
                          : "In Stock";
            string color = p.Quantity == 0 ? "#dc3545"
                          : p.Quantity <= p.ReorderLevel ? "#856404"
                          : "#146c43";
            string bg = p.Quantity == 0 ? "#f8d7da"
                          : p.Quantity <= p.ReorderLevel ? "#fff3cd"
                          : "#d1e7dd";

            rows.Append($"<tr><td>{Esc(p.ProductName)}</td><td>{Esc(p.SKU)}</td>" +
                        $"<td><strong>{p.Quantity}</strong></td><td>{p.ReorderLevel}</td>" +
                        $"<td><span style='background:{bg};color:{color};padding:2px 8px;border-radius:4px;font-size:11px;'>{status}</span></td></tr>");
        }

        return PdfResult(
            BuildPage("Live Stock Report",
                "<tr><th>Product</th><th>SKU</th><th>Available</th><th>Reorder</th><th>Status</th></tr>",
                rows.ToString(), $"Total active products: {data.Count}", false),
            $"LiveStock_{Stamp()}.pdf", false);
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

        var rows = new StringBuilder();
        foreach (var s in data)
            rows.Append($"<tr><td>{Esc(s.SupplierName)}</td><td>{Esc(s.Email)}</td>" +
                        $"<td>{Esc(s.Phone)}</td><td>{Esc(s.Address)}</td></tr>");

        return PdfResult(
            BuildPage("Suppliers Report",
                "<tr><th>Supplier Name</th><th>Email</th><th>Phone</th><th>Address</th></tr>",
                rows.ToString(), $"Total: {data.Count}", false),
            $"Suppliers_{Stamp()}.pdf", false);
    }

    // ─────────────────────────────────────────────────────────────────
    // INVOICES
    // INVEXA_new Invoice uses CustomerName/ProductName strings (no FK nav)
    // ─────────────────────────────────────────────────────────────────

    public IActionResult InvoicesExcel()
    {
        var data = _context.Invoices
            .OrderByDescending(i => i.InvoiceDate)
            .ToList();

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
        var data = _context.Invoices
            .OrderByDescending(i => i.InvoiceDate)
            .ToList();

        var rows = new StringBuilder();
        foreach (var inv in data)
        {
            string badgeStyle = inv.PaymentStatus == "Paid"
                ? "background:#d1e7dd;color:#146c43;"
                : "background:#fff3cd;color:#856404;";
            rows.Append($"<tr><td>{Esc(inv.InvoiceNumber)}</td>" +
                        $"<td>{inv.InvoiceDate.ToLocalTime():dd-MM-yyyy}</td>" +
                        $"<td>{Esc(inv.CustomerName)}</td><td>{Esc(inv.ProductName)}</td>" +
                        $"<td>{inv.Quantity}</td><td>₹{inv.TotalAmount:N2}</td>" +
                        $"<td><span style='{badgeStyle}padding:2px 6px;border-radius:4px;font-size:11px;'>{Esc(inv.PaymentStatus)}</span></td></tr>");
        }

        return PdfResult(
            BuildPage("Invoice Report",
                "<tr><th>Invoice</th><th>Date</th><th>Customer</th><th>Product</th><th>Qty</th><th>Total</th><th>Status</th></tr>",
                rows.ToString(), $"Total invoices: {data.Count}", true),
            $"Invoices_{Stamp()}.pdf", true);
    }

    // Single invoice formatted document
    public IActionResult InvoicePdf(int id)
    {
        var inv = _context.Invoices.Find(id);
        if (inv is null) return NotFound();

        string html = $@"<!DOCTYPE html><html><head><meta charset= 'UTF-8'><style>
body{{font-family:DejaVu Sans, sans-serif;font-size:13px;padding:40px;color:#333}}
.top{{display:table;width:100%;margin-bottom:30px}}
.brand{{display:table-cell;vertical-align:top}}
.brand-name{{font - size: 26px;
    font-weight: 900;
    letter-spacing: 6px;
    color: #005461;
    font-family: Arial Black, Arial, sans-serif;
    text-transform: uppercase;}}
.brand-name span {{color: #249db0ff;}}
.meta{{display:table-cell;vertical-align:top;text-align:right;font-size:12px}}
.meta h3{{color:#0F3040;margin:0 0 6px;font-size:20px}}
.badge{{display:inline-block;background:#d1e7dd;color:#146c43;padding:3px 12px;border-radius:4px;font-size:12px}}
.box{{background:#f8f9fa;padding:14px 18px;border-radius:6px;margin-bottom:24px}}
table{{width:100%;border-collapse:collapse;margin-top:8px}}
th{{background:#0F3040;color:white;padding:9px;text-align:left}}
td{{padding:9px;border-bottom:1px solid #dee2e6}}
.tot td{{font-weight:bold;font-size:15px;background:#eef2ff}}
.footer{{margin-top:40px;font-size:11px;color:#aaa;text-align:center;border-top:1px solid #dee2e6;padding-top:12px}}
</style></head><body>
<div class='top'>
  <div class='brand'><div class='brand-name'><h3>INV<span>EXA</span></h3></div><div style='font-size:12px;color:#888'>Inventory Management</div></div>
  <div class='meta'><h3>INVOICE</h3><div>{Esc(inv.InvoiceNumber)}</div><div>Date: {inv.InvoiceDate.ToLocalTime():dd MMM yyyy}</div><div class='badge'>{Esc(inv.PaymentStatus)}</div></div>
</div>
<div class='box'><div style='font-size:11px;text-transform:uppercase;color:#888;margin-bottom:4px'>Billed To</div>
<strong>{Esc(inv.CustomerName)}</strong><br/>
{(string.IsNullOrEmpty(inv.Phone) ? "" : $"Phone: {Esc(inv.Phone)}<br/>")}{(string.IsNullOrEmpty(inv.Email) ? "" : $"Email: {Esc(inv.Email)}")}
</div>
<table><thead><tr><th>#</th><th>Product</th><th>Qty</th><th>Unit Price</th><th>Total</th></tr></thead>
<tbody>
<tr><td>1</td><td>{Esc(inv.ProductName)}</td><td>{inv.Quantity}</td><td>₹{inv.UnitPrice:N2}</td><td>₹{inv.TotalAmount:N2}</td></tr>
<tr class='tot'><td colspan='4' style='text-align:right'>Total Amount</td><td>₹{inv.TotalAmount:N2}</td></tr>
</tbody></table>
<div class='footer'>Generated by INVEXA &nbsp;|&nbsp; {DateTime.Now.ToLocalTime():dd MMM yyyy, hh:mm tt}</div>
</body></html>";

        return PdfResult(html, $"{inv.InvoiceNumber}.pdf", false);
    }

    // ─────────────────────────────────────────────────────────────────
    // SALES
    // New in INVEXA_new — not in old project
    // ─────────────────────────────────────────────────────────────────

    public IActionResult SalesExcel()
    {
        var data = _context.Sales
            .OrderByDescending(s => s.SaleDate)
            .Take(500)   // ponytail: cap at 500 rows; add pagination if needed
            .ToList();

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
        var data = _context.Sales
            .OrderByDescending(s => s.SaleDate)
            .Take(500)
            .ToList();

        var rows = new StringBuilder();
        foreach (var s in data)
        {
            string color = s.Status == "Voided" ? "#6b7280" : "#146c43";
            string bg = s.Status == "Voided" ? "#f3f4f6" : "#d1e7dd";
            rows.Append($"<tr><td>{s.SaleDate.ToLocalTime():dd-MM-yyyy HH:mm}</td>" +
                        $"<td>{Esc(s.ProductName)}</td><td>{Esc(s.CustomerName)}</td>" +
                        $"<td>{s.Quantity}</td><td>₹{s.TotalAmount:N2}</td>" +
                        $"<td><span style='background:{bg};color:{color};padding:2px 6px;border-radius:4px;font-size:11px;'>{Esc(s.Status)}</span></td></tr>");
        }

        return PdfResult(
            BuildPage("Sales Report",
                "<tr><th>Date</th><th>Product</th><th>Customer</th><th>Qty</th><th>Total</th><th>Status</th></tr>",
                rows.ToString(), $"Total: {data.Count}", true),
            $"Sales_{Stamp()}.pdf", true);
    }

    // ─────────────────────────────────────────────────────────────────
    // PURCHASE ORDERS
    // New in INVEXA_new — not in old project
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

        var rows = new StringBuilder();
        foreach (var po in data)
        {
            string color = po.Status == "Received" ? "#146c43"
                         : po.Status == "Sent" ? "#1e40af"
                         : "#6b7280";
            string bg = po.Status == "Received" ? "#d1e7dd"
                         : po.Status == "Sent" ? "#dbeafe"
                         : "#f3f4f6";
            var items = string.Join(", ", po.Items.Select(i =>
                $"{Esc(i.Product?.ProductName ?? "?")} ×{i.OrderedQuantity}"));

            rows.Append($"<tr><td>{Esc(po.PurchaseOrderNumber)}</td>" +
                        $"<td>{Esc(po.Supplier?.SupplierName)}</td>" +
                        $"<td>{items}</td>" +
                        $"<td>{po.CreatedAt.ToLocalTime():dd-MM-yyyy}</td>" +
                        $"<td><span style='background:{bg};color:{color};padding:2px 6px;border-radius:4px;font-size:11px;'>{Esc(po.Status)}</span></td></tr>");
        }

        return PdfResult(
            BuildPage("Purchase Orders Report",
                "<tr><th>PO Number</th><th>Supplier</th><th>Items</th><th>Created</th><th>Status</th></tr>",
                rows.ToString(), $"Total POs: {data.Count}", true),
            $"PurchaseOrders_{Stamp()}.pdf", true);
    }

    // ─────────────────────────────────────────────────────────────────
    // SHARED HELPERS
    // ─────────────────────────────────────────────────────────────────

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

    private string BuildPage(string heading, string headers, string rows, string footer, bool landscape)
    => $@"<!DOCTYPE html><html><head><meta charset='UTF-8'><style>
body{{font-family:Arial,sans-serif;font-size:{(landscape ? 12 : 13)}px;padding:20px;line-height:1.5}}
.report-top{{display:table;width:100%;margin-bottom:18px;border-bottom:2px solid #005461;padding-bottom:14px}}
.brand-cell{{display:table-cell;vertical-align:middle}}
.brand-name{{font-size:22px;font-weight:900;letter-spacing:6px;color:#0F3040;font-family:Arial Black,Arial,sans-serif;text-transform:uppercase}}
.brand-name span{{color:#249db0ff}}
.brand-sub{{font-size:11px;color:#888;margin-top:2px}}
.title-cell{{display:table-cell;vertical-align:middle;text-align:right}}
.title-cell h2{{color:#0F3040;margin:0 0 3px;font-size:{(landscape ? 17 : 19)}px}}
.title-cell p{{color:#666;font-size:11px;margin:0}}
table{{width:100%;border-collapse:collapse;margin-top:16px;table-layout: fixed}}
th{{background:#0F3040;color:white;padding:10px 8px;text-align:left;white-space: nowrap;}}
td{{padding:9px 8px;border-bottom:1px solid #dee2e6;word-wrap: break-word;
    overflow-wrap: break-word;letter-spacing:0.01em}}
tr:nth-child(even) td{{background:#f8f9fa}}
.footer{{margin-top:20px;font-size:11px;color:#888}}
</style></head><body>
<div class='report-top'>
  <div class='brand-cell'>
    <div class='brand-name'>INV<span>EXA</span></div>
    <div class='brand-sub'>Inventory Management</div>
  </div>
  <div class='title-cell'>
    <h2>{heading}</h2>
    <p>Generated: {DateTime.Now.ToLocalTime():dd MMM yyyy, hh:mm tt}</p>
  </div>
</div>
<table><thead>{headers}</thead><tbody>{rows}</tbody></table>
<div class='footer'>{footer}</div>
</body></html>";


    private FileContentResult PdfResult(string html, string filename, bool landscape)
    {
        var doc = new HtmlToPdfDocument
        {
            GlobalSettings = new GlobalSettings
            {
                PaperSize = PaperKind.A4,
                Orientation = landscape ? Orientation.Landscape : Orientation.Portrait,
                Margins = new MarginSettings { Top = 15, Bottom = 15, Left = 15, Right = 15 }
            },
            Objects = { new ObjectSettings { HtmlContent = html } }
        };
        return File(_pdf.Convert(doc), "application/pdf", filename);
    }

    // XSS-safe HTML escaping for PDF content
    private static string Esc(string? s)
        => System.Net.WebUtility.HtmlEncode(s ?? "—");

    private static string Stamp() => DateTime.Now.ToString("yyyyMMdd");
}
