using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public interface ISupplierProductService
{
    Task<Supplier> GetOrCreateSupplierAsync(string supplierName);
    Task<Product> GetOrCreateProductAsync(Guid supplierId, string productCode, string productName, string? unit);
}

public class SupplierProductService : ISupplierProductService
{
    private readonly AppDbContext _context;

    public SupplierProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Supplier> GetOrCreateSupplierAsync(string supplierName)
    {
        // Try to find existing supplier
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Name == supplierName);

        if (supplier == null)
        {
            // Create new supplier
            supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = supplierName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }

        return supplier;
    }

    public async Task<Product> GetOrCreateProductAsync(Guid supplierId, string productCode, string productName, string? unit)
    {
        // Try to find existing product with compound key (SupplierId + ProductCode)
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.SupplierId == supplierId && p.ProductCode == productCode);

        if (product == null)
        {
            // Create new product
            product = new Product
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                ProductCode = productCode,
                Name = productName,
                CurrentUnit = unit,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update product name and unit if they've changed
            var updated = false;
            
            if (product.Name != productName)
            {
                product.Name = productName;
                updated = true;
            }
            
            if (!string.IsNullOrEmpty(unit) && product.CurrentUnit != unit)
            {
                product.CurrentUnit = unit;
                updated = true;
            }

            if (updated)
            {
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return product;
    }
}
