using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;
using DinkToPdf;
using DinkToPdf.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddHttpClient();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

var app = builder.Build();

// Apply any pending EF Core migrations automatically on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Ensure the supplier-login column exists (safe & idempotent - never breaks startup)
    try
    {
        db.Database.ExecuteSqlRaw(
            "IF COL_LENGTH('Admins','SupplierId') IS NULL ALTER TABLE Admins ADD SupplierId INT NULL;");
        db.Database.ExecuteSqlRaw(
            "IF COL_LENGTH('Products','ImageUrl') IS NULL ALTER TABLE Products ADD ImageUrl NVARCHAR(1000) NULL;");
        db.Database.ExecuteSqlRaw(
            "IF COL_LENGTH('Products','CreatedAt') IS NULL ALTER TABLE Products ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME();");
        db.Database.ExecuteSqlRaw(
            "IF COL_LENGTH('Admins','ProfilePhoto') IS NULL ALTER TABLE Admins ADD ProfilePhoto NVARCHAR(MAX) NULL;");
    }
    catch { /* best-effort */ }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.Run();
