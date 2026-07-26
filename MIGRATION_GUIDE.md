# INVEXA → INVEXA_new Feature Migration Guide

This document compares the two codebases, identifies features present in the old INVEXA project
that are missing from INVEXA_new, and provides step-by-step implementation instructions for each.

---

## Codebase Summary

> Last updated from git log — commits `60e2966` (role based access), `41c2b5f` (preload/login/register/security question), `fdc6791` (completed inventory system).

| Aspect | INVEXA (old) | INVEXA_new | Status |
|---|---|---|---|
| Auth | Plain-text passwords, session-based | PBKDF2-hashed passwords, session-based | ✅ Already better |
| Roles | Admin / Staff with role-based access | ✅ Admin / User — fully wired (`60e2966`) | ✅ Done (see note) |
| Password security | Plain text stored in DB | PBKDF2 + SHA512, 120k iterations | ✅ Already better |
| Dashboard | Hardcoded stat numbers | Real DB queries + charts + KPIs | ✅ Already better |
| Stock management | Stock In (via Invoice) + Stock Out | Stock Adjust (generic +/- delta) | ✅ Already better |
| Invoice flow | Standalone supplier invoice → triggers Stock In | Invoice auto-generated from Sale checkout | ✅ Already better |
| Purchase Orders | Not present | Full PO workflow (Draft → Sent → Received) | ✅ Already better |
| Sales | Removed intentionally | Full Sales module with void support | ✅ Already better |
| Reports | Not implemented (placeholder) | Revenue, profit, dead stock, reorder | ✅ Already better |
| Export (Excel/PDF) | ClosedXML + DinkToPdf — full implementation | Not present | ❌ Missing |
| Global Search | AJAX search across Products/Categories/Suppliers | Not present | ❌ Missing |
| Notifications | Full system — model, helper, controller, view | Not present | ❌ Missing |
| Login History | Model + captured on login | Not present | ❌ Missing |
| Forgot Password | Security question 3-step flow | Model fields done, 3-step flow missing | ⚠️ Partial |
| Admin Login page | Separate `/Account/AdminLogin` | Not present | ❌ Missing |
| Staff registration | Self-registration with security question | ✅ Done — bootstrap guard removed (`41c2b5f`) | ✅ Done (see note) |
| Security question fields | On Admin model | ✅ Added — migration run (`41c2b5f`) | ✅ Done |
| Theme preference | Persisted to DB per user | Not present | ❌ Missing |
| Last login | Stored on Admin model | Not present | ❌ Missing |
| BaseController | Populates ViewData["UnreadCount"] per request | Not present | ❌ Missing |
| `[SessionAuthorize]` | Supports role parameter `("Admin")` | ✅ Role parameter added (`60e2966`) | ✅ Done |
| AccessDenied page | `/Account/AccessDenied` | ✅ Added (`60e2966`) | ✅ Done |
| Product model | Basic (Name, Category string, Price, Qty) | Rich (SKU, CostPrice, ReorderLevel, SupplierId FK, IsActive, etc.) | ✅ Already better |
| Supplier–Product FK | Invoice has SupplierId + ProductId FKs | Supplier directly linked to Product | ✅ Already better |
| `OnModelCreating` | No precision config, no explicit FKs | Full precision config + all FK behaviors defined | ✅ Already better |

> **Role name note:** INVEXA_new uses `"User"` where old INVEXA used `"Staff"`. The `[SessionAuthorize("Admin")]`
> checks work correctly. However any view-layer role checks using `== "Staff"` from old code must use `== "User"` instead.
> Consider standardising to `"Staff"` via a single-column migration to avoid confusion.

---

## Features to Port from INVEXA → INVEXA_new

---

### Feature 1 — Role-Based Access Control (Admin / Staff)

**Status in INVEXA_new:** ✅ COMPLETE — implemented in commit `60e2966`.

**What was done:**
- `Filters/SessionAuthorizeAttribute.cs` — role parameter added, `return` after redirect fixed
- `Controllers/AccountController.cs` — `AccessDenied()` action added
- `Views/Account/AccessDenied.cshtml` — created
- All controller mutations decorated with `[SessionAuthorize("Admin")]`:
  - `ProductController` — Create×2, Edit×2, Delete, Archive
  - `CategoryController` — Create×2, Edit×2, Delete, DeleteConfirmed
  - `SupplierController` — Create×2, Edit×2, Delete, DeleteConfirmed
  - `SalesController` — Void
  - `PurchaseOrderController` — Create×2, MarkSent, Receive
  - `StockController` — Adjust×2
- `_AdminLayout.cshtml` — PurchaseOrders link wrapped in `@if (sessionRole == "Admin")`, role label reads from session

**One remaining gap — Reports link not conditional:**
`ReportController.Index` has no `[SessionAuthorize("Admin")]` and the Reports sidebar link
is not wrapped in an `@if (sessionRole == "Admin")` block. Staff can currently access Reports.

Fix needed in `_AdminLayout.cshtml`:
```cshtml
@if (sessionRole == "Admin")
{
    <li><a class="@act("Report")" asp-controller="Report" asp-action="Index">
        <i class="bi bi-bar-chart"></i> Reports &amp; Insights</a></li>
}
```

Fix needed in `Controllers/ReportController.cs` — add to `Index`:
```csharp
[SessionAuthorize("Admin")]
public async Task<IActionResult> Index() { ... }
```

**Role name discrepancy — action required:**
Self-registered users get `Role = "User"` (set in `AccountController.Register`, commit `41c2b5f`).
Old INVEXA used `"Staff"`. The `[SessionAuthorize("Admin")]` checks are unaffected but any
future view-layer `== "Staff"` checks from ported code will silently fail.

Two options:
- **Option A (recommended):** Run a migration to rename existing `"User"` values to `"Staff"` and update `Register` POST to hardcode `Role = "Staff"`.
- **Option B:** Accept `"User"` as the role name and update all ported view checks to `== "User"`.

---

### Feature 2 — Forgot Password (Security Question Flow)

**Status in INVEXA_new:** ⚠️ PARTIAL — model fields and migration done (`41c2b5f`); 3-step flow not built yet.

**What is already done:**
- `Models/Admin.cs` — `SecurityQuestion` and `SecurityAnswer` fields added (`[StringLength(200)]`, nullable)
- `Migrations/20260726053040_AddSecurityQuestion.cs` — migration already run, columns exist in DB
- `AccountController.Register` POST — captures security question + answer, normalises answer to lowercase
- `Views/Account/Register.cshtml` — security question dropdown present in UI

**What still needs to be built:**

**Step 1 — Add preset questions list to `AccountController`:**
```csharp
public static readonly List<string> SecurityQuestions = new()
{
    "What was the name of your first pet?",
    "What is your mother's maiden name?",
    "What was the name of your primary school?",
    "What is your favourite movie?",
    "What city were you born in?",
    "What is the name of your childhood best friend?",
    "What was your childhood nickname?"
};
```
Pass to Register view via `ViewBag.SecurityQuestions = SecurityQuestions;` in Register GET.

**Step 2 — Add 3 action pairs to `AccountController`:**
- `ForgotPassword` GET/POST — accepts username, validates it exists, stores `"ResetUsername"` in session, redirects to step 2
- `VerifyQuestion` GET/POST — reads user's security question, verifies submitted answer with `.ToLowerInvariant().Trim()` comparison, sets `Session["ResetVerified"] = "true"`, redirects to step 3
- `ResetPassword` GET/POST — requires `Session["ResetVerified"] == "true"` guard, hashes new password via `PasswordSecurity.Hash()`, clears `ResetUsername` and `ResetVerified` session keys

**Step 3 — Create 3 views:**
- `Views/Account/ForgotPassword.cshtml` — username input, uses `_LoginLayout`
- `Views/Account/VerifyQuestion.cshtml` — shows stored question, answer input, uses `_LoginLayout`
- `Views/Account/ResetPassword.cshtml` — new password + confirm inputs, uses `_LoginLayout`

**Step 4 — Add Forgot Password link to Login view:**
```cshtml
<a asp-action="ForgotPassword">Forgot Password?</a>
```

**No migration needed** — `SecurityQuestion` and `SecurityAnswer` columns already exist.

---

### Feature 3 — Separate Admin Login Page

**Status in INVEXA_new:** ❌ Missing. Single `/Account/Login` page handles all roles with no separation.

**What INVEXA has:**
- `/Account/Login` — blocks Admin credentials, shows error "Admin accounts must use the Admin Login page"
- `/Account/AdminLogin` — blocks non-Admin credentials
- Two toggle buttons above both login cards

**Implementation steps:**

**Step 1 — Add `AdminLogin` GET/POST to `AccountController`:**
```csharp
[HttpGet]
public IActionResult AdminLogin() =>
    HttpContext.Session.GetString("Username") is null ? View(new Admin()) : RedirectToAction("Index", "Home");

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AdminLogin(Admin input)
{
    var user = await _context.Admins.FirstOrDefaultAsync(a => a.Username == input.Username.Trim());
    if (user is null || !PasswordSecurity.Verify(input.Password, user.Password) || user.Role != "Admin")
    {
        ViewBag.Error = "Invalid credentials or not an Admin account.";
        return View(input);
    }
    HttpContext.Session.SetString("Username", user.Username);
    HttpContext.Session.SetInt32("AdminId", user.Id);
    HttpContext.Session.SetString("Role", user.Role);
    return RedirectToAction("Index", "Home");
}
```

**Step 2 — Update existing `Login` POST to block Admin:**
After `PasswordSecurity.Verify` passes, add:
```csharp
if (user.Role == "Admin")
{
    ViewBag.Error = "Admin accounts must use the Admin Login page.";
    return View(input);
}
```

**Step 3 — Create `Views/Account/AdminLogin.cshtml`:**
Copy structure from `Login.cshtml`. Change:
- No Register link
- `asp-action="AdminLogin"` on the form
- Different heading/icon to distinguish visually

**Step 4 — Add toggle buttons to both login views:**
Two buttons above the card — "Staff Login" linking to `/Account/Login`, "Admin Login" linking to `/Account/AdminLogin`. Active button highlighted based on which page is current.

---

### Feature 4 — Staff Self-Registration

**Status in INVEXA_new:** ✅ COMPLETE — implemented in commit `41c2b5f`.

**What was done:**
- Bootstrap guard (`if AnyAsync() return Forbid()`) removed from Register GET and POST
- Registration now open at all times
- Security question + answer captured and stored
- Answer normalised to `.Trim().ToLowerInvariant()` before saving

**One discrepancy vs old INVEXA:**
New registrations get `Role = "User"` — old INVEXA used `"Staff"`.
See Feature 1 role name note for resolution options.

---

### Feature 5 — Notification System

**Status in INVEXA_new:** Not present at all.

**What INVEXA has:**
- `Models/Notification.cs` — `Id`, `Message`, `Category`, `ForRole`, `IsRead`, `CreatedAt`
- `Helpers/NotificationHelper.cs` — static `Add(context, message, category, forRole)` method
- `Controllers/BaseController.cs` — populates `ViewData["UnreadCount"]` on every request
- `Controllers/NotificationController.cs` — Index (with category tabs), MarkRead, MarkAllRead, ClearAll
- `Views/Notification/Index.cshtml` — full page with category filter tabs, list, mark read/clear buttons
- Bell icon in layout shows real unread count badge
- All controllers insert notifications at key events

**Implementation steps:**

**Step 1 — Create `Models/Notification.cs`:**
```csharp
public class Notification
{
    public int Id { get; set; }
    [Required] public string Message { get; set; }
    [Required] public string Category { get; set; }  // Inventory|Account|Staff|Supplier|Alerts
    public string ForRole { get; set; } = "All";     // All|Admin|Staff
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Step 2 — Add to `Data/ApplicationDbContext.cs`:**
```csharp
public DbSet<Notification> Notifications { get; set; }
```

**Step 3 — Run migration:**
```
dotnet ef migrations add AddNotifications
dotnet ef database update
```

**Step 4 — Create `Helpers/NotificationHelper.cs`:**
Copy directly from old project. No changes needed.

**Step 5 — Create `Controllers/BaseController.cs`:**
Copy from old project. Change `_context.Notifications.Count(...)` to use async if desired.

**Step 6 — Update all controllers to inherit `BaseController`:**
Change `Controller` → `BaseController` for: `HomeController`, `ProductController`, `CategoryController`,
`SupplierController`, `StockController`, `InvoiceController`, `SalesController`, `PurchaseOrderController`, `ReportController`.
Remove the duplicate `_context` field and constructor from each — `BaseController` provides both.

**Step 7 — Create `Controllers/NotificationController.cs`:**
Copy from old project. No changes needed — already uses `BaseController`.

**Step 8 — Create `Views/Notification/Index.cshtml`:**
Copy from old project. Works as-is.

**Step 9 — Add notification insertions to controllers:**
Copy the `NotificationHelper.Add(...)` call patterns from old project into each controller at the appropriate events:

| Controller | Event | Category | ForRole |
|---|---|---|---|
| AccountController | Login (staff) | Account | Admin |
| AccountController | Register | Staff | Admin |
| AccountController | ResetPassword | Account | All |
| ProductController | Create | Inventory | Admin |
| ProductController | Edit | Inventory | Admin |
| ProductController | Archive/Delete | Inventory | Admin |
| CategoryController | Create/Edit/Delete | Inventory | Admin |
| SupplierController | Create/Edit/Delete | Supplier | Admin |
| StockController | Adjust (positive) | Inventory | All |
| StockController | Adjust (negative, low result) | Alerts | All |
| SalesController | Create (sale) | Inventory | All |
| SalesController | Void | Inventory | Admin |
| PurchaseOrderController | Receive (goods in) | Inventory | All |

**Step 10 — Update bell in `_AdminLayout.cshtml`:**
Replace hardcoded notification dropdown with:
```cshtml
<a asp-controller="Notification" asp-action="Index"
   class="btn btn-light position-relative me-3 text-decoration-none">
    <i class="bi bi-bell fs-5"></i>
    @if (ViewData["UnreadCount"] is int count && count > 0)
    {
        <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
            @(count > 99 ? "99+" : count.ToString())
        </span>
    }
</a>
```

---

### Feature 6 — Export (Excel + PDF)

**Status in INVEXA_new:** Not present.

**What INVEXA has:**
`Controllers/ExportController.cs` — exports for Products, Stock Overview, Stock History, Invoices (list + individual), Suppliers.
Uses `ClosedXML` for Excel and `DinkToPdf` for PDF.

**Implementation steps:**

**Step 1 — Install NuGet packages:**
```
dotnet add package ClosedXML
dotnet add package DinkToPdf
```

**Step 2 — Copy `libwkhtmltox.dll` to project root:**
Copy from old project root. Add to `StockFlow.csproj`:
```xml
<ItemGroup>
    <None Update="libwkhtmltox.dll">
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

**Step 3 — Register DinkToPdf in `Program.cs`:**
```csharp
using DinkToPdf;
using DinkToPdf.Contracts;
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
```

**Step 4 — Adapt `ExportController.cs` from old project:**
The old `ExportController` references `p.Category` (a string), but INVEXA_new uses `p.CategoryName` (nullable string).
Update all category references:
```csharp
// Old:
ws.Cell(i + 2, 3).Value = data[i].Category;
// New:
ws.Cell(i + 2, 3).Value = data[i].CategoryName ?? data[i].Category?.CategoryName ?? "-";
```

The old `StockMovement` uses `m.MovementDate` and `m.Reason` — INVEXA_new uses `m.CreatedAt` and `m.Notes`.
Update StockHistory export methods:
```csharp
// Old:
data[i].MovementDate  → data[i].CreatedAt
data[i].Reason        → data[i].Notes
data[i].Quantity      → data[i].QuantityDelta  (or Math.Abs(data[i].QuantityDelta))
```

Invoice export: INVEXA_new Invoice model uses `CustomerName` string (not FK). Update accordingly:
```csharp
ws.Cell(i + 2, 3).Value = data[i].CustomerName;  // no Supplier nav property in INVEXA_new
ws.Cell(i + 2, 4).Value = data[i].ProductName;    // no Product nav property
```

**Step 5 — Add export buttons to views:**
Add Excel/PDF buttons to `Views/Product/Index.cshtml`, `Views/Stock/Index.cshtml`,
`Views/Invoice/Index.cshtml`, `Views/Supplier/Index.cshtml`.

---

### Feature 7 — Global Search

**Status in INVEXA_new:** Not present.

**What INVEXA has:**
`Controllers/SearchController.cs` — AJAX endpoint `/Search/Global?q=term` returning JSON with Products, Categories, Suppliers.
Topbar search box with live dropdown results using fetch + debounce.

**Implementation steps:**

**Step 1 — Create `Controllers/SearchController.cs`:**
Copy from old project. Update product query — old uses `p.Category` (string), new uses `p.CategoryName`:
```csharp
var products = _context.Products
    .AsEnumerable()
    .Where(p => p.IsActive &&
        (p.ProductName.ToLower().Replace(" ", "").Contains(term) ||
         (p.CategoryName ?? "").ToLower().Replace(" ", "").Contains(term)))
    .Select(p => new { p.Id, p.ProductName, Category = p.CategoryName ?? "-" })
    .Take(6)
    .ToList();
```

**Step 2 — Update topbar in `_AdminLayout.cshtml`:**
Replace the static search input with:
```html
<div class="me-3 position-relative">
    <div class="input-group">
        <span class="input-group-text bg-white"><i class="bi bi-search"></i></span>
        <input type="text" id="globalSearch" class="form-control"
               placeholder="Search anything..." autocomplete="off">
    </div>
    <div id="globalSearchResults"
         class="position-absolute bg-white border rounded shadow-sm w-100 d-none"
         style="top:100%;z-index:1050;max-height:400px;overflow-y:auto;min-width:320px;">
    </div>
</div>
```

**Step 3 — Add search JS script to layout:**
Copy the `globalSearch` script block from old `_AdminLayout.cshtml` — handles debounce (300ms),
fetch call, result rendering with group headers, click-outside dismiss, and XSS escaping.

---

### Feature 8 — Login History

**Status in INVEXA_new:** Not present.

**What INVEXA has:**
- `Models/LoginHistory.cs` — `Id`, `AdminId` (FK), `LoginTime`, `IpAddress`
- Inserted on every successful login in `AccountController`
- Shown on Profile page (last 10 entries)

**Implementation steps:**

**Step 1 — Create `Models/LoginHistory.cs`:**
```csharp
public class LoginHistory
{
    public int Id { get; set; }
    [Required] public int AdminId { get; set; }
    [ForeignKey("AdminId")] public Admin Admin { get; set; }
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

**Step 2 — Add to `ApplicationDbContext`:**
```csharp
public DbSet<LoginHistory> LoginHistory { get; set; }
```

**Step 3 — Add migration:**
```
dotnet ef migrations add AddLoginHistory
dotnet ef database update
```

**Step 4 — Insert in `AccountController.Login` POST after session is set:**
```csharp
_context.LoginHistory.Add(new LoginHistory
{
    AdminId   = user.Id,
    LoginTime = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
});
await _context.SaveChangesAsync();
```

**Step 5 — Show on Profile page:**
Query in `AccountController.Profile` GET:
```csharp
ViewBag.LoginHistory = await _context.LoginHistory
    .Where(h => h.AdminId == user.Id)
    .OrderByDescending(h => h.LoginTime)
    .Take(10)
    .ToListAsync();
```

---

### Feature 9 — User Profile Enhancements

**Status in INVEXA_new:** Basic profile — only shows username, no edit, no password change.

**What INVEXA has:**
- Edit username
- Change password (current + new + confirm)
- Theme preference (light/dark persisted to DB)
- Last login timestamp
- Recent activity (last 10 notifications)
- Login history

**Implementation steps:**

**Step 1 — Add fields to `Models/Admin.cs`:**
```csharp
public DateTime? LastLogin { get; set; }
public string ThemePreference { get; set; } = "light";
```

**Step 2 — Run migration:**
```
dotnet ef migrations add AddAdminProfileFields
dotnet ef database update
```

**Step 3 — Update `AccountController.Login` POST:**
```csharp
user.LastLogin = DateTime.UtcNow;
HttpContext.Session.SetString("Theme", user.ThemePreference ?? "light");
await _context.SaveChangesAsync();
```

**Step 4 — Add `Profile` POST, `ChangePassword` GET/POST, `SaveTheme` GET:**
Copy these actions from old `AccountController`. Adapt `ChangePassword` to use `PasswordSecurity.Verify()`
and `PasswordSecurity.Hash()` — INVEXA_new already has these in `Security/PasswordSecurity.cs`.

**Step 5 — Replace `Views/Account/Profile.cshtml`:**
Use the structured layout from old project:
- Header card: avatar icon + username + role badge + last login
- Edit profile form (username + theme select)
- Change password form (current + new + confirm)
- Recent activity list (from Notifications — requires Feature 5)
- Login history list (requires Feature 8)

**Step 6 — Add `SaveTheme` endpoint and JS in layout:**
```csharp
[HttpGet]
public async Task<IActionResult> SaveTheme(string theme)
{
    var adminId = HttpContext.Session.GetInt32("AdminId");
    var user = await _context.Admins.FindAsync(adminId);
    if (user != null)
    {
        user.ThemePreference = theme == "dark" ? "dark" : "light";
        await _context.SaveChangesAsync();
        HttpContext.Session.SetString("Theme", user.ThemePreference);
    }
    return Ok();
}
```

---

### Feature 10 — Manage Users (Admin only)

**Status in INVEXA_new:** Not present. No user management at all.

**What INVEXA has:**
- `/Account/Users` — list all accounts with role badges
- `/Account/DeleteUser` — Admin can delete any account except their own

**Implementation steps:**

**Step 1 — Add `Users`, `DeleteUser` GET/POST to `AccountController`:**
Copy from old project. Requires Feature 1 (role-based access) — decorate with `[SessionAuthorize("Admin")]`.

**Step 2 — Create `Views/Account/Users.cshtml`:**
Copy from old project. Shows username, role badge (Admin = red, Staff = green), delete button.
Self-account shows "Cannot delete own account" instead of delete button.

**Step 3 — Add "Manage Users" to sidebar in `_AdminLayout.cshtml`:**
```cshtml
@if (Context.Session.GetString("Role") == "Admin")
{
    <li>
        <a asp-controller="Account" asp-action="Users">
            <i class="bi bi-people"></i>
            <span>Manage Users</span>
        </a>
    </li>
}
```

---

## Implementation Order (Recommended)

| # | Feature | Status | Reason for order |
|---|---|---|---|
| 1 | Role-based access | ✅ Done (one gap: Reports link) | Fix Reports link + role name discrepancy first |
| 2 | Staff self-registration | ✅ Done | — |
| 3 | Forgot Password (3-step flow) | ⚠️ Partial — build the 3 actions + views | Model + migration already done |
| 4 | Separate Admin login page | ❌ Build next | Auth flow cleaner before adding more features |
| 5 | Manage Users | ❌ Build next | Requires role-based access (done) |
| 6 | Login History | ❌ Standalone | Add to login flow alongside profile work |
| 7 | Profile enhancements | ❌ Requires login history + notifications | Do after 6 and 8 |
| 8 | Notification system + BaseController | ❌ Most files | Required by profile recent activity |
| 9 | Global Search | ❌ Independent | Wire up existing search input in topbar |
| 10 | Export (Excel + PDF) | ❌ Independent | Do any time |

---

## Key Model Differences to Watch

When porting code, these field name differences will cause compile errors if not updated:

| Old INVEXA | INVEXA_new | Note |
|---|---|---|
| `Product.Category` (string) | `Product.CategoryName` (nullable string) | Both exist as legacy column |
| `StockMovement.Quantity` | `StockMovement.QuantityDelta` | Sign indicates direction in new |
| `StockMovement.Reason` | `StockMovement.Notes` | Renamed |
| `StockMovement.MovementDate` | `StockMovement.CreatedAt` | Renamed |
| `Invoice.SupplierId` (FK) | `Invoice.CustomerName` (string) | New project kept string for customer-facing |
| `Invoice.ProductId` (FK) | `Invoice.ProductName` (string) | Same |
| `Admin.Password` (plain text) | `Admin.Password` (PBKDF2 hash) | Use `PasswordSecurity.Verify/Hash` |
| No `Product.IsActive` | `Product.IsActive` | Filter active products in all queries |

---

## Files to Copy Directly (No or Minimal Changes)

These files can be copied from old INVEXA with only the model field name fixes above:

| File | Status | Changes needed when copying |
|---|---|---|
| `Helpers/NotificationHelper.cs` | ❌ Missing | None — copy as-is |
| `Controllers/BaseController.cs` | ❌ Missing | None — copy as-is |
| `Controllers/NotificationController.cs` | ❌ Missing | None — copy as-is |
| `Views/Notification/Index.cshtml` | ❌ Missing | None — copy as-is |
| `Views/Account/AccessDenied.cshtml` | ✅ Done | Already present |
| `Views/Account/Users.cshtml` | ❌ Missing | Role check: `"Staff"` → `"User"` if not standardising |
| `Views/Account/ForgotPassword.cshtml` | ❌ Missing | None — copy as-is |
| `Views/Account/VerifyQuestion.cshtml` | ❌ Missing | None — copy as-is |
| `Views/Account/ResetPassword.cshtml` | ❌ Missing | Use `PasswordSecurity.Hash()` instead of plain assignment |
| `Controllers/ExportController.cs` | ❌ Missing | Field name fixes (see Feature 6) |
| `Controllers/SearchController.cs` | ❌ Missing | `p.Category` → `p.CategoryName` fix |
| `libwkhtmltox.dll` | ❌ Missing | Copy to project root + csproj entry |
