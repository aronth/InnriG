using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // Get all products with supplier info and latest prices
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] Guid? supplierId = null)
    {
        var query = _context.Products.AsQueryable();

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        // Get products with their latest invoice item in a single query
        var result = await query
            .GroupJoin(
                _context.InvoiceItems.Include(ii => ii.Invoice),
                p => p.Id,
                ii => ii.ProductId,
                (product, items) => new
                {
                    Product = product,
                    LatestItem = items.OrderByDescending(ii => ii.Invoice.InvoiceDate).FirstOrDefault()
                })
            .Select(p => new ProductListDto
            {
                Id = p.Product.Id,
                ProductCode = p.Product.ProductCode,
                Name = p.Product.Name,
                Description = p.Product.Description,
                CurrentUnit = p.Product.CurrentUnit,
                SupplierId = p.Product.SupplierId,
                SupplierName = p.Product.Supplier.Name,
                LatestPrice = p.LatestItem != null ? p.LatestItem.UnitPrice : null,
                ListPrice = p.LatestItem != null ? p.LatestItem.ListPrice : null,
                Discount = p.LatestItem != null ? p.LatestItem.Discount : null,
                DiscountPercentage = p.LatestItem != null && p.LatestItem.ListPrice > 0
                    ? (p.LatestItem.Discount / p.LatestItem.ListPrice) * 100
                    : null,
                LastPurchaseDate = p.LatestItem != null ? p.LatestItem.Invoice.InvoiceDate : null
            })
            .OrderBy(p => p.SupplierName)
            .ThenBy(p => p.Name)
            .ToListAsync();
        
        return Ok(result);
    }

    // Get product by ID with price history
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.Supplier)
            .Include(p => p.InvoiceItems)
                .ThenInclude(ii => ii.Invoice)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (product == null) 
            return NotFound();
        
        return Ok(product);
    }

    // Get product price history
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetProductHistory(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        var history = await _context.InvoiceItems
            .Where(ii => ii.ProductId == id)
            .Include(ii => ii.Invoice)
            .OrderByDescending(ii => ii.Invoice.InvoiceDate)
            .Select(ii => new
            {
                InvoiceId = ii.InvoiceId,
                InvoiceDate = ii.Invoice.InvoiceDate,
                InvoiceNumber = ii.Invoice.InvoiceNumber,
                Quantity = ii.Quantity,
                Unit = ii.Unit,
                ListPrice = ii.ListPrice,
                Discount = ii.Discount,
                UnitPrice = ii.UnitPrice,
                TotalPrice = ii.TotalPrice
            })
            .ToListAsync();

        return Ok(new
        {
            Product = product,
            History = history
        });
    }

    // Compare prices across suppliers for similar product codes
    [HttpGet("compare")]
    public async Task<IActionResult> CompareProductPrices([FromQuery] string productCode)
    {
        if (string.IsNullOrEmpty(productCode))
            return BadRequest("Product code is required");

        // Get all products matching this code from different suppliers
        var products = await _context.Products
            .Include(p => p.Supplier)
            .Where(p => p.ProductCode == productCode)
            .ToListAsync();

        if (!products.Any())
            return NotFound("No products found with this code");

        // Get latest price for each product
        var priceComparison = new List<object>();
        
        foreach (var product in products)
        {
            var latestItem = await _context.InvoiceItems
                .Where(ii => ii.ProductId == product.Id)
                .Include(ii => ii.Invoice)
                .OrderByDescending(ii => ii.Invoice.InvoiceDate)
                .FirstOrDefaultAsync();

            if (latestItem != null)
            {
                priceComparison.Add(new
                {
                    ProductId = product.Id,
                    ProductCode = product.ProductCode,
                    ProductName = product.Name,
                    SupplierId = product.SupplierId,
                    SupplierName = product.Supplier.Name,
                    LatestPrice = latestItem.UnitPrice,
                    ListPrice = latestItem.ListPrice,
                    Discount = latestItem.Discount,
                    LastPurchaseDate = latestItem.Invoice.InvoiceDate,
                    Unit = latestItem.Unit
                });
            }
        }

        return Ok(priceComparison.OrderBy(p => ((dynamic)p).LatestPrice));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if supplier exists
        if (!await _context.Suppliers.AnyAsync(s => s.Id == product.SupplierId))
            return BadRequest("Supplier not found");

        // Check for duplicate (SupplierId + ProductCode)
        if (await _context.Products.AnyAsync(p => p.SupplierId == product.SupplierId && p.ProductCode == product.ProductCode))
            return BadRequest("Product with this code already exists for this supplier");

        product.Id = Guid.NewGuid();
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] Product product)
    {
        if (id != product.Id)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        product.UpdatedAt = DateTime.UtcNow;
        
        _context.Entry(product).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
