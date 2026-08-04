# INVEXA — Inventory Management System

A full-featured inventory management web application built with **ASP.NET Core MVC** and **SQL Server**. INVEXA covers the complete inventory lifecycle — from product and supplier management to sales, invoicing, purchase orders, and business analytics.

---

## Live Demo

> Hosted on Azure App Service
> **URL:** (https://invexaproject-awhkaddda2bfefhx.centralindia-01.azurewebsites.net/)

---

## 🔑 Demo Credentials

| Role | Username | Password | Login Tab |
|------|----------|----------|-----------|
| **Admin** | `Admin` | `Admin@123` | Admin tab |

> Admin has full access to all features. Staff can view data and process sales but cannot modify master records or access reports.

---

## Features

### Inventory
- Product catalog with SKU, cost price, sell price, reorder level, and supplier linking
- Category management
- Supplier management with supplier portal access
- Live stock view with low stock and out-of-stock alerts
- Immutable stock movement ledger — every quantity change is tracked

### Sales & Invoicing
- Process sales with automatic stock deduction
- Auto-generated invoices on every sale
- Void sales with stock reversal via ledger entry
- Export invoices as individual PDFs

### Purchase Orders
- Full PO lifecycle: Draft → Sent → Received
- Automatic stock update and cost price update on receipt
- Reorder suggestions based on current stock levels

### Reports & Analytics
- Live dashboard with KPIs, charts, and inventory health meter
- Revenue, gross profit, dead stock, and reorder intelligence
- Sales trend, top products, and category breakdown charts

### Exports
- PDF and Excel exports for Products, Live Stock, Suppliers, Invoices, Sales, and Purchase Orders

### Search
- Global AJAX search across Products, Categories, and Suppliers from the navigation bar
- Page-level instant search on Products, Suppliers, and Stock pages

### Security
- PBKDF2-SHA512 password hashing (120,000 iterations)
- Role-based access control (Admin / Staff / Supplier)
- Brute-force protection — account locked for 15 minutes after 5 failed login attempts
- CSRF protection on all forms
- Separate login tabs per role — cross-role login blocked server-side

### Supplier Portal
- Dedicated login for suppliers
- Suppliers can view only their own purchase orders

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core MVC (.NET 8) |
| Database | SQL Server + Entity Framework Core 8 |
| Authentication | Custom session-based auth with PBKDF2 |
| PDF Export | QuestPDF |
| Excel Export | ClosedXML |
| Frontend | Bootstrap 5, Bootstrap Icons, Chart.js |
| Fonts | Orbitron, Plus Jakarta Sans, Inter (Google Fonts) |
| Hosting | Azure App Service |

---

## Project Structure

```
INVEXA_new/
├── Controllers/
│   ├── AccountController.cs      # Auth, register, forgot password, user management
│   ├── ProductController.cs      # Product CRUD + image fetch
│   ├── CategoryController.cs     # Category CRUD
│   ├── SupplierController.cs     # Supplier CRUD
│   ├── SalesController.cs        # Sales + void
│   ├── InvoiceController.cs      # Invoice view
│   ├── StockController.cs        # Live stock + adjustments + ledger
│   ├── PurchaseOrderController.cs# PO lifecycle
│   ├── ReportController.cs       # Reports & analytics
│   ├── SearchController.cs       # Global search API
│   ├── ExportController.cs       # PDF + Excel exports
│   ├── NotificationController.cs # Notification feed
│   ├── SupplierPortalController.cs# Supplier-facing portal
│   ├── HomeController.cs         # Dashboard
│   └── BaseController.cs         # Shared unread notification count
├── Models/                       # EF Core entity models + ViewModels
├── Data/
│   └── ApplicationDbContext.cs   # DbContext + OnModelCreating config
├── Filters/
│   └── SessionAuthorizeAttribute.cs # Custom auth filter
├── Security/
│   └── PasswordSecurity.cs       # PBKDF2 hash + verify
├── Helpers/
│   └── NotificationHelper.cs     # Notification creation helper
├── Services/
│   └── InventoryInsights.cs      # Reorder suggestion logic
├── Views/                        # Razor views per controller
├── wwwroot/
│   ├── css/login.css             # Login/register page styles
│   └── images/                   # Product images, logos, backgrounds
└── Migrations/                   # EF Core migrations
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or remote) — [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) is free
- [SQL Server Management Studio](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (optional)

### Setup

**1. Clone the repository**
```bash
git clone https://github.com/your-username/INVEXA.git
cd INVEXA
```

**2. Configure the connection string**

Open `appsettings.json` and update the connection string to match your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=InvexaDB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;"
  }
}
```

**3. Apply database migrations**

Make sure your SQL Server instance is running, then run:

```bash
dotnet ef database update
```

This creates the database and all tables from the existing migrations.

> If `dotnet ef` is not installed, install it first:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

**4. Run the application**
```bash
dotnet watch
```

Navigate to `https://localhost:{port}` and log in with the Admin credentials above.

> **Note:** The app also calls `db.Database.Migrate()` automatically on startup, so even if you skip step 3, migrations will be applied on first run. Running step 3 manually is recommended to catch any connection issues before starting the app.

---

## Database

The application uses **EF Core Code First** migrations. The database is created and migrated automatically on startup via:

```csharp
db.Database.Migrate();
```

### Key Tables

| Table | Purpose |
|-------|---------|
| `Admins` | All user accounts (Admin, Staff, Supplier roles) |
| `Products` | Product catalog |
| `Categories` | Product categories |
| `Suppliers` | Supplier master |
| `Sales` | Outbound sales transactions |
| `Invoices` | Auto-generated invoices (1:1 with Sales) |
| `StockMovements` | Immutable stock ledger |
| `PurchaseOrders` | Inbound order headers |
| `PurchaseOrderItems` | Inbound order line items |
| `Notifications` | System alert feed |
| `LoginHistory` | Login audit trail |

---

## Role Permissions

| Feature | Admin | Staff | Supplier |
|---------|-------|-------|----------|
| Dashboard | ✅ | ✅ | ❌ |
| View Products / Stock | ✅ | ✅ | ❌ |
| Create / Edit / Archive Products | ✅ | ❌ | ❌ |
| Process Sales | ✅ | ✅ | ❌ |
| Void Sales | ✅ | ❌ | ❌ |
| View Invoices | ✅ | ✅ | ❌ |
| Purchase Orders | ✅ | View only | Own POs only |
| Reports & Analytics | ✅ | ❌ | ❌ |
| Manage Users | ✅ | ❌ | ❌ |
| Export PDF / Excel | ✅ | ✅ | ❌ |
| Supplier Portal | ❌ | ❌ | ✅ |

---

## Screenshots

><img width="1600" height="900" alt="WhatsApp Image 2026-08-03 at 5 24 27 PM" src="https://github.com/user-attachments/assets/63d940c7-6e22-4eee-8198-130f53678097" />
> <img width="940" height="529" alt="image" src="https://github.com/user-attachments/assets/d28d0702-5911-43b6-af77-3a27ef9dc520" />
<img width="1600" height="900" alt="WhatsApp Image 2026-08-03 at 6 43 38 PM" src="https://github.com/user-attachments/assets/ef35917c-dd4c-4b2f-9dab-82565cdb2fa2" />
<img width="1600" height="900" alt="image" src="https://github.com/user-attachments/assets/a8b654f1-7015-4089-bdab-d8a373a8ffb3" />
<img width="940" height="407" alt="image" src="https://github.com/user-attachments/assets/42ee1e62-f235-4861-b146-074284fe7cf7" />






---

## License

This project is for educational and portfolio purposes.
