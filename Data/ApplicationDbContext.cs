using Microsoft.EntityFrameworkCore;
using StockFlow.Models;

namespace StockFlow.Data;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Category> Categories { get; set; }
    
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<LoginHistory> LoginHistory { get; set; }
    public DbSet<Notification> Notifications { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(p => p.CostPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<PurchaseOrderItem>().Property(i => i.UnitCost).HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier).WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Product).WithMany(p => p.StockMovements)
            .HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LoginHistory>()
            .HasOne(h => h.Admin).WithMany()
            .HasForeignKey(h => h.AdminId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoginHistory>()
            .HasIndex(h => new { h.AdminId, h.LoginTime });

        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Product).WithMany(p => p.Sales)
            .HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Sale).WithOne(s => s.Invoice)
            .HasForeignKey<Invoice>(i => i.SaleId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(p => p.Supplier).WithMany(s => s.PurchaseOrders)
            .HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.PurchaseOrder).WithMany(p => p.Items)
            .HasForeignKey(i => i.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.Product).WithMany(p => p.PurchaseOrderItems)
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.ForRole, n.IsRead, n.CreatedAt });

        modelBuilder.Entity<Product>().HasIndex(p => p.SKU);
        modelBuilder.Entity<Sale>().HasIndex(s => new { s.ProductId, s.SaleDate });
        modelBuilder.Entity<StockMovement>().HasIndex(s => new { s.ProductId, s.CreatedAt });
    }
}
