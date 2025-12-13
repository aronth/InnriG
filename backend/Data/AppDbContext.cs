using InnriGreifi.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Supplier Configuration
        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.Name)
            .IsUnique();
        
        // Product Configuration
        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.SupplierId, p.ProductCode })
            .IsUnique();
        
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Invoice Configuration
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Supplier)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalAmount)
            .HasPrecision(18, 2);
        
        // InvoiceItem Configuration
        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Product)
            .WithMany(p => p.InvoiceItems)
            .HasForeignKey(ii => ii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.ListPrice)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.TotalPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.TotalPriceWithVat)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.Discount)
            .HasPrecision(18, 2);
            
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.Quantity)
            .HasPrecision(18, 3);
    }
}
