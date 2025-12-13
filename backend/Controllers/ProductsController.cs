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
    public async Task<IActionResult> GetProducts(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? hasPrice = null,
        [FromQuery] string? sortBy = "supplierName",
        [FromQuery] string? sortOrder = "asc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100; // Limit max page size

        // Build base query with latest invoice items
        var baseQuery = _context.Products
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
            });

        // Apply filters
        if (supplierId.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.SupplierId == supplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            baseQuery = baseQuery.Where(p => 
                p.Name.ToLower().Contains(searchLower) ||
                p.ProductCode.ToLower().Contains(searchLower) ||
                (p.Description != null && p.Description.ToLower().Contains(searchLower)));
        }

        if (minPrice.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.LatestPrice.HasValue && p.LatestPrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.LatestPrice.HasValue && p.LatestPrice <= maxPrice.Value);
        }

        if (hasPrice.HasValue)
        {
            if (hasPrice.Value)
            {
                baseQuery = baseQuery.Where(p => p.LatestPrice.HasValue);
            }
            else
            {
                baseQuery = baseQuery.Where(p => !p.LatestPrice.HasValue);
            }
        }

        // Apply sorting
        baseQuery = sortBy.ToLower() switch
        {
            "name" => sortOrder.ToLower() == "desc" 
                ? baseQuery.OrderByDescending(p => p.Name)
                : baseQuery.OrderBy(p => p.Name),
            "productcode" => sortOrder.ToLower() == "desc"
                ? baseQuery.OrderByDescending(p => p.ProductCode)
                : baseQuery.OrderBy(p => p.ProductCode),
            "suppliername" => sortOrder.ToLower() == "desc"
                ? baseQuery.OrderByDescending(p => p.SupplierName).ThenByDescending(p => p.Name)
                : baseQuery.OrderBy(p => p.SupplierName).ThenBy(p => p.Name),
            "latestprice" => sortOrder.ToLower() == "desc"
                ? baseQuery.OrderByDescending(p => p.LatestPrice ?? decimal.MinValue)
                : baseQuery.OrderBy(p => p.LatestPrice ?? decimal.MaxValue),
            "lastpurchasedate" => sortOrder.ToLower() == "desc"
                ? baseQuery.OrderByDescending(p => p.LastPurchaseDate ?? DateTime.MinValue)
                : baseQuery.OrderBy(p => p.LastPurchaseDate ?? DateTime.MaxValue),
            _ => baseQuery.OrderBy(p => p.SupplierName).ThenBy(p => p.Name) // Default sorting
        };

        // Get total count before pagination
        var totalCount = await baseQuery.CountAsync();

        // Apply pagination
        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new PaginatedResponse<ProductListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    // Lookup products for autocomplete (requires 3+ characters)
    [HttpGet("lookup")]
    public async Task<IActionResult> LookupProducts([FromQuery] string q, [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
        {
            return BadRequest("Query must be at least 3 characters long");
        }

        if (limit < 1) limit = 1;
        if (limit > 50) limit = 50; // Limit max results

        var queryLower = q.ToLower();

        var results = await _context.Products
            .Where(p => 
                p.Name.ToLower().Contains(queryLower) ||
                p.ProductCode.ToLower().Contains(queryLower))
            .GroupJoin(
                _context.InvoiceItems.Include(ii => ii.Invoice),
                p => p.Id,
                ii => ii.ProductId,
                (product, items) => new
                {
                    Product = product,
                    LatestItem = items.OrderByDescending(ii => ii.Invoice.InvoiceDate).FirstOrDefault()
                })
            .Select(p => new ProductLookupDto
            {
                Id = p.Product.Id,
                ProductCode = p.Product.ProductCode,
                Name = p.Product.Name,
                SupplierName = p.Product.Supplier.Name,
                LatestPrice = p.LatestItem != null ? p.LatestItem.UnitPrice : null
            })
            .OrderBy(p => p.Name)
            .Take(limit)
            .ToListAsync();

        return Ok(results);
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

    // Compare multiple products by IDs with full price history
    [HttpPost("compare")]
    public async Task<IActionResult> CompareMultipleProducts([FromBody] List<Guid> productIds)
    {
        if (productIds == null || !productIds.Any())
            return BadRequest("At least one product ID is required");

        if (productIds.Count > 10)
            return BadRequest("Maximum 10 products can be compared at once");

        var comparisonResults = new List<object>();

        foreach (var productId in productIds)
        {
            var product = await _context.Products
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                continue;

            var latestItem = await _context.InvoiceItems
                .Where(ii => ii.ProductId == productId)
                .Include(ii => ii.Invoice)
                .OrderByDescending(ii => ii.Invoice.InvoiceDate)
                .FirstOrDefaultAsync();

            var history = await _context.InvoiceItems
                .Where(ii => ii.ProductId == productId)
                .Include(ii => ii.Invoice)
                .OrderBy(ii => ii.Invoice.InvoiceDate)
                .Select(ii => new
                {
                    InvoiceDate = ii.Invoice.InvoiceDate,
                    UnitPrice = ii.UnitPrice,
                    ListPrice = ii.ListPrice,
                    Discount = ii.Discount,
                    Quantity = ii.Quantity,
                    Unit = ii.Unit
                })
                .ToListAsync();

            comparisonResults.Add(new
            {
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.Name,
                Description = product.Description,
                SupplierId = product.SupplierId,
                SupplierName = product.Supplier.Name,
                LatestPrice = latestItem?.UnitPrice,
                ListPrice = latestItem?.ListPrice,
                Discount = latestItem?.Discount,
                LastPurchaseDate = latestItem?.Invoice.InvoiceDate,
                Unit = latestItem?.Unit ?? product.CurrentUnit,
                History = history
            });
        }

        return Ok(comparisonResults);
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

        // Check if any invoice items reference this product
        var hasInvoiceItems = await _context.InvoiceItems
            .AnyAsync(ii => ii.ProductId == id);

        if (hasInvoiceItems)
        {
            return BadRequest(new { 
                message = "Ekki er hægt að eyða vöru sem er notuð í reikningum. Eyða verður fyrst öllum reikningum sem innihalda þessa vöru." 
            });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
