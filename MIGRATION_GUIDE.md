# INVEXA → INVEXA_new Feature Migration Guide

This document compares the two codebases, identifies features present in the old INVEXA project
that are missing from INVEXA_new, and provides step-by-step implementation instructions for each.

---

## Codebase Summary

| Aspect | INVEXA (old) | INVEXA_new |
|---|---|---|
| Auth | Plain-text passwords, session-based | PBKDF2-hashed passwords, session-based |
| Roles | Admin / Staff with role-based access | Single role only (no role separation) |
| Password security | Plain text stored in DB | PBKDF2 + SHA512, 120k iterations |
| Dashboard | Hardcoded stat numbers | Real DB queries + charts + KPIs |
| Stock management | Stock In (via Invoice) + Stock Out | Stock Adjust (generic +/- delta) |
| Invoice flow | Standalone supplier invoice → triggers Stock In | Invoice auto-generated from Sale checkout |
| Purchase Orders | Not present | Full PO workflow (Draft → Sent → Received) |
| Sales | Removed intentionally | Full Sales module with void support |
| Reports | Not implemented (placeholder) | Revenue, profit, dead stock, reorder |
| Export (Excel/PDF) | ClosedXML + DinkToPdf — full implementation | Not present |
| Global Search | AJAX search across Products/Categories/Suppliers | Not present |
| Notifications | Full system — model, helper, controller, view | Not present |
| Login History | Model + captured on login | Not present |
| Forgot Password | Security question flow (3-step) | Not present |
| Admin Login page | Separate `/Account/AdminLogin` | Not present |
| Staff registration | Self-registration on login page | One-time bootstrap only |
| Theme preference | Persisted to DB per user | Not present |
| Last login | Stored on Admin model | Not present |
| BaseController | Populates ViewData["UnreadCount"] per request | Not present |
| `[SessionAuthorize]` | Supports role parameter `("Admin")` | No role parameter support |
| Product model | Basic (Name, Category string, Price, Qty) | Rich (SKU, CostPrice, ReorderLevel, SupplierId FK, IsActive, etc.) |
| Supplier–Product FK | Invoice has SupplierId + ProductId FKs | Supplier directly linked to Product |
| `OnModelCreating` | No precision config, no explicit FKs | Full precision config + all FK behaviors defined |

---

## Features to Port from INVEXA → INVEXA_new

---

### Feature 1 — Role-Based Access Control (Admin / Staff)

**Status in INVEXA_new:** Missing. `SessionAuthorizeAttribute` only checks if logged in — no role parameter.

**What INVEXA has:**
- Two roles: `Admin` and `Staff`
- `[SessionAuthorize("Admin")]` restricts actions to Admin only
- Staff blocked from: deleting products/categories/suppliers, deleting invoices, managing users
- Sidebar hides admin-only links based on session role
- Separate Admin login page (`/Account/AdminLogin`)

**Implementation steps:**

**Step 1 — Update `Filters/SessionAuthorizeAttribute.cs`:**
Add an optional role parameter to the constructor:
```csharp
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute(string? role = null)
    {
        _requiredRole = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var username = context.HttpContext.Session.GetString("Username");

        if (string.IsNullOrEmpty(username))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (!string.IsNullOrEmpty(_requiredRole))
        {
            var sessionRole = context.HttpContext.Session.GetString("Role");
            if (sessionRole != _requiredRole)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
```

**Step 2 — Add `Role` field to `Models/Admin.cs`:**
Already present but default is `"Admin"`. Change to support both `"Admin"` and `"Staff"`.

**Step 3 — Store role in session on login in `AccountController`:**
```csharp
HttpContext.Session.SetString("Role", user.Role);
```

**Step 4 — Decorate controller actions:**
- `ProductController.Create/Edit/Delete` → `[SessionAuthorize("Admin")]`
- `CategoryController.Create/Edit/Delete` → `[SessionAuthorize("Admin")]`
- `SupplierController.Create/Edit/Delete` → `[SessionAuthorize("Admin")]`
- `SalesController.Void` → `[SessionAuthorize("Admin")]`
- User management actions → `[SessionAuthorize("Admin")]`

**Step 5 — Add `AccessDenied` action to `AccountController`:**
```csharp
public IActionResult AccessDenied() => View();
```
Create `Views/Account/AccessDenied.cshtml` with a "403 — Insufficient permissions" message.

**Step 6 — Conditional UI in views:**
In `_AdminLayout.cshtml`, wrap admin-only sidebar links:
```cshtml
@if (Context.Session.GetString("Role") == "Admin")
{
    <li><a asp-controller="Account" asp-action="Users">Manage Users</a></li>
}
```

---

### Feature 2 — Forgot Password (Security Question Flow)

**Status in INVEXA_new:** Missing entirely.

**What INVEXA has:**
3-step flow: Enter username → Answer security question → Set new password.
Works without email/SMTP — suitable for offline/assignment use.

**Implementation steps:**

**Step 1 — Add fields to `Models/Admin.cs`:**
```csharp
public string? SecurityQuestion { get; set; }
public string? SecurityAnswer   { get; set; }  // stored lowercase + trimmed
```

**Step 2 — Run migration:**
```
dotnet ef migrations add AddSecurityQuestion
dotnet ef database update
```

**Step 3 — Add preset questions list to `AccountController`:**
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

**Step 4 — Add 3 action pairs to `AccountController`:**
- `ForgotPassword` GET/POST — accepts username, stores it in session as `"ResetUsername"`, redirects to step 2
- `VerifyQuestion` GET/POST — shows security question, verifies answer (`.ToLower().Trim()` comparison), sets `Session["ResetVerified"] = "true"`, redirects to step 3
- `ResetPassword` GET/POST — requires `Session["ResetVerified"] == "true"`, updates password, clears reset session keys

**Step 5 — Create 3 views:**
- `Views/Account/ForgotPassword.cshtml`
- `Views/Account/VerifyQuestion.cshtml`
- `Views/Account/ResetPassword.cshtml`

All three use `_LoginLayout` and show the Staff/Admin toggle buttons above the card.

**Step 6 — Add Forgot Password link to Login views:**
```cshtml
<a asp-action="ForgotPassword" asp-route-type="staff">Forgot Password?</a>
```

---

### Feature 3 — Separate Admin Login Page

**Status in INVEXA_new:** Missing. Single login page, no role separation at entry point.

**What INVEXA has:**
- `/Account/Login` — Staff only. Blocks Admin credentials with error message.
- `/Account/AdminLogin` — Admin only. Blocks Staff credentials.
- Two toggle buttons above both login cards linking to each page.

**Implementation steps:**

**Step 1 — Add `AdminLogin` GET/POST to `AccountController`:**
```csharp
[HttpGet]
public IActionResult AdminLogin()
{
    if (HttpContext.Session.GetString("Username") != null)
        return RedirectToAction("Index", "Home");
    return View();
}

[HttpPost]
public IActionResult AdminLogin(Admin admin)
{
    var user = _context.Admins.FirstOrDefault(x =>
        x.Username == admin.Username && x.Password == admin.Password);

    if (user == null || user.Role != "Admin")
    {
        ViewBag.Error = "Invalid credentials or not an Admin account.";
        return View(admin);
    }
    // NOTE: Use PasswordSecurity.Verify() instead of plain == for INVEXA_new

    HttpContext.Session.SetString("Username", user.Username);
    HttpContext.Session.SetString("Role", user.Role);
    return RedirectToAction("Index", "Home");
}
```

**Step 2 — Update Staff Login POST to block Admin:**
```csharp
if (user.Role == "Admin")
{
    ViewBag.Error = "Admin accounts must use the Admin Login page.";
    return View(admin);
}
```

**Step 3 — Create `Views/Account/AdminLogin.cshtml`:**
Same structure as `Login.cshtml` but:
- No Register link
- Title: "Administrator Portal"
- Different icon (`bi-shield-lock-fill`)
- Toggle buttons above card (Staff Login / Admin Login — Admin button is `active`)

**Step 4 — Update `Views/Account/Login.cshtml`:**
Add two toggle buttons above the card and a Forgot Password link.
Staff Login button has `active` class, Admin Login button links to `/Account/AdminLogin`.

---

### Feature 4 — Staff Self-Registration

**Status in INVEXA_new:** Register is a one-time bootstrap. Redirects away if any admin exists.

**What INVEXA has:**
Staff can self-register at any time. Role is hardcoded to `"Staff"`. Admin sees new registrations in Manage Users.

**Implementation steps:**

**Step 1 — Update `AccountController.Register` GET:**
Remove the `if (await _context.Admins.AnyAsync()) return RedirectToAction(nameof(Login));` guard.
Allow registration regardless of existing accounts.

**Step 2 — Update `AccountController.Register` POST:**
- Remove the `if (await _context.Admins.AnyAsync()) return Forbid();` guard
- Hardcode `Role = "Staff"` regardless of form input
- Add security question + answer fields
- Normalize answer: `admin.SecurityAnswer = admin.SecurityAnswer?.ToLower().Trim();`
- Use `PasswordSecurity.Hash()` — already present in INVEXA_new

**Step 3 — Update `Views/Account/Register.cshtml`:**
Add security question `<select>` dropdown (populated from `ViewBag.SecurityQuestions`) and answer input field.
Add toggle buttons above card consistent with Login/AdminLogin pages.

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

Do these in order — each builds on the previous:

| # | Feature | Reason for order |
|---|---|---|
| 1 | Role-based access (`SessionAuthorize` update) | Everything else depends on roles |
| 2 | Separate Admin login + Staff registration | Auth flow must be correct first |
| 3 | Forgot Password | Requires security question fields on Admin model |
| 4 | Manage Users | Requires role-based access |
| 5 | Login History | Standalone, add to login flow |
| 6 | Profile enhancements | Requires login history + notifications (do after 7) |
| 7 | Notification system + BaseController | Required by profile recent activity |
| 8 | Global Search | Independent, do any time |
| 9 | Export (Excel + PDF) | Independent, do any time |

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

| File | Changes needed |
|---|---|
| `Helpers/NotificationHelper.cs` | None |
| `Controllers/BaseController.cs` | None |
| `Controllers/NotificationController.cs` | None |
| `Views/Notification/Index.cshtml` | None |
| `Views/Account/AccessDenied.cshtml` | None |
| `Views/Account/Users.cshtml` | None |
| `Views/Account/ForgotPassword.cshtml` | None |
| `Views/Account/VerifyQuestion.cshtml` | None |
| `Views/Account/ResetPassword.cshtml` | None |
| `Controllers/ExportController.cs` | Field name fixes (see Feature 6) |
| `Controllers/SearchController.cs` | `p.Category` → `p.CategoryName` fix |
| `libwkhtmltox.dll` | Copy to project root |
