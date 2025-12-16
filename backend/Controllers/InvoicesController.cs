using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceParser _parser;
    private readonly AppDbContext _context;
    private readonly ISupplierProductService _supplierProductService;

    public InvoicesController(IInvoiceParser parser, AppDbContext context, ISupplierProductService supplierProductService)
    {
        _parser = parser;
        _context = context;
        _supplierProductService = supplierProductService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadInvoice(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        using var reader = new StreamReader(file.OpenReadStream());
        var htmlContent = await reader.ReadToEndAsync();

        try
        {
            var invoice = _parser.ParseInvoice(htmlContent, file.FileName);
            
            // Check if invoice already exists (by supplier + invoice number)
            // First, get or create supplier to check against existing invoices
            var supplier = await _supplierProductService.GetOrCreateSupplierAsync(invoice.SupplierName);
            
            var existingInvoice = await _context.Invoices
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(i => i.SupplierId == supplier.Id && i.InvoiceNumber == invoice.InvoiceNumber);
            
            // Return invoice with duplicate flag
            var response = new
            {
                id = invoice.Id,
                supplierName = invoice.SupplierName,
                buyerName = invoice.BuyerName,
                buyerTaxId = invoice.BuyerTaxId,
                invoiceNumber = invoice.InvoiceNumber,
                invoiceDate = invoice.InvoiceDate,
                totalAmount = invoice.TotalAmount,
                items = invoice.Items,
                createdAt = invoice.CreatedAt,
                isDuplicate = existingInvoice != null,
                existingInvoiceId = existingInvoice?.Id,
                existingInvoiceDate = existingInvoice?.InvoiceDate
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error parsing invoice: {ex.Message}");
        }
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmInvoice([FromBody] InvoiceConfirmDto invoiceDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Debug: Log received BuyerTaxId
        Console.WriteLine($"[ConfirmInvoice] Received BuyerTaxId: '{invoiceDto.BuyerTaxId ?? "null"}' (Length: {invoiceDto.BuyerTaxId?.Length ?? 0})");
        Console.WriteLine($"[ConfirmInvoice] Received BuyerName: '{invoiceDto.BuyerName ?? "null"}'");

        // 1. Get or create supplier
        var supplier = await _supplierProductService.GetOrCreateSupplierAsync(invoiceDto.SupplierName);
        
        // 1.5. Get or create buyer (if TaxId provided)
        Guid? buyerId = null;
        if (!string.IsNullOrWhiteSpace(invoiceDto.BuyerTaxId))
        {
            try
            {
                // Get or create buyer by TaxId - the service handles normalization internally
                var buyer = await _supplierProductService.GetOrCreateBuyerAsync(
                    invoiceDto.BuyerTaxId,
                    invoiceDto.BuyerName
                );
                
                buyerId = buyer.Id;
                Console.WriteLine($"[ConfirmInvoice] Successfully got/created buyer. BuyerId: {buyerId}, TaxId: '{buyer.TaxId}'");
            }
            catch (Exception ex)
            {
                // Log error with full details
                Console.WriteLine($"[ConfirmInvoice] ERROR creating/getting buyer:");
                Console.WriteLine($"  TaxId: '{invoiceDto.BuyerTaxId}'");
                Console.WriteLine($"  Name: '{invoiceDto.BuyerName}'");
                Console.WriteLine($"  Exception: {ex.Message}");
                Console.WriteLine($"  Stack: {ex.StackTrace}");
                
                // Return error - we need the buyer to be set
                return BadRequest(new { 
                    message = $"Villa við að búa til eða sækja kaupanda: {ex.Message}",
                    buyerTaxId = invoiceDto.BuyerTaxId,
                    error = ex.Message
                });
            }
        }
        else
        {
            Console.WriteLine($"[ConfirmInvoice] WARNING: BuyerTaxId is empty or whitespace. BuyerId will be null.");
        }
        
        // 2. Check for duplicate invoice (same supplier + invoice number)
        var existingInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.SupplierId == supplier.Id && i.InvoiceNumber == invoiceDto.InvoiceNumber);
        
        if (existingInvoice != null)
        {
            return Conflict(new { 
                message = $"Reikningur með númeri {invoiceDto.InvoiceNumber} frá {invoiceDto.SupplierName} er þegar til í kerfinu.",
                invoiceId = existingInvoice.Id,
                invoiceNumber = existingInvoice.InvoiceNumber,
                invoiceDate = existingInvoice.InvoiceDate
            });
        }
        
        // 3. Create invoice entity
        var invoice = new Invoice
        {
            Id = invoiceDto.Id == Guid.Empty ? Guid.NewGuid() : invoiceDto.Id,
            SupplierId = supplier.Id,
            BuyerId = buyerId,
            SupplierName = invoiceDto.SupplierName,
            BuyerName = invoiceDto.BuyerName,
            BuyerTaxId = invoiceDto.BuyerTaxId,
            InvoiceNumber = invoiceDto.InvoiceNumber,
            InvoiceDate = invoiceDto.InvoiceDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(invoiceDto.InvoiceDate, DateTimeKind.Utc)
                : invoiceDto.InvoiceDate.Kind == DateTimeKind.Local 
                    ? invoiceDto.InvoiceDate.ToUniversalTime()
                    : invoiceDto.InvoiceDate,
            TotalAmount = invoiceDto.TotalAmount,
            CreatedAt = DateTime.UtcNow
        };
        
        Console.WriteLine($"[ConfirmInvoice] Created invoice entity with BuyerId: {buyerId?.ToString() ?? "null"}");
        
        // 4. Process items: get or create products and create InvoiceItem entities
        foreach (var itemDto in invoiceDto.Items)
        {
            // Get or create product with compound key (SupplierId + ProductCode)
            var product = await _supplierProductService.GetOrCreateProductAsync(
                supplier.Id,
                itemDto.ItemId,
                itemDto.ItemName,
                itemDto.Unit
            );
            
            var invoiceItem = new InvoiceItem
            {
                Id = itemDto.Id == Guid.Empty ? Guid.NewGuid() : itemDto.Id,
                InvoiceId = invoice.Id,
                ProductId = product.Id,
                ItemName = itemDto.ItemName,
                ItemId = itemDto.ItemId,
                Quantity = itemDto.Quantity,
                Unit = itemDto.Unit,
                UnitPrice = itemDto.UnitPrice,
                ListPrice = itemDto.ListPrice,
                VatCode = itemDto.VatCode,
                Discount = itemDto.Discount,
                TotalPrice = itemDto.TotalPrice,
                TotalPriceWithVat = itemDto.TotalPriceWithVat
            };
            
            invoice.Items.Add(invoiceItem);
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        
        // Verify the buyer was saved
        var savedInvoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        Console.WriteLine($"[ConfirmInvoice] Invoice saved. BuyerId in DB: {savedInvoice?.BuyerId?.ToString() ?? "null"}");

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    [HttpPost("bulk-upload")]
    public async Task<IActionResult> BulkUploadInvoices([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files provided");

        var results = new BulkUploadResult
        {
            Total = files.Count,
            Successful = 0,
            Skipped = 0,
            Failed = 0,
            Errors = new List<string>()
        };

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
            {
                results.Failed++;
                results.Errors.Add($"{file?.FileName ?? "Unknown"}: File is empty");
                continue;
            }

            try
            {
                // Parse invoice
                using var reader = new StreamReader(file.OpenReadStream());
                var htmlContent = await reader.ReadToEndAsync();
                var parsedInvoice = _parser.ParseInvoice(htmlContent, file.FileName);

                // Convert parsed invoice to DTO
                var invoiceDto = new InvoiceConfirmDto
                {
                    Id = parsedInvoice.Id,
                    SupplierName = parsedInvoice.SupplierName,
                    BuyerName = parsedInvoice.BuyerName,
                    BuyerTaxId = parsedInvoice.BuyerTaxId,
                    InvoiceNumber = parsedInvoice.InvoiceNumber,
                    InvoiceDate = parsedInvoice.InvoiceDate,
                    TotalAmount = parsedInvoice.TotalAmount,
                    Items = parsedInvoice.Items.Select(item => new InvoiceItemDto
                    {
                        Id = item.Id,
                        ItemName = item.ItemName,
                        ItemId = item.ItemId,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        ListPrice = item.ListPrice,
                        VatCode = item.VatCode,
                        Discount = item.Discount,
                        TotalPrice = item.TotalPrice,
                        TotalPriceWithVat = item.TotalPriceWithVat
                    }).ToList()
                };

                // Try to save invoice (this will skip duplicates)
                var saveResult = await SaveInvoiceAsync(invoiceDto);
                
                if (saveResult.IsSuccess)
                {
                    results.Successful++;
                }
                else if (saveResult.IsDuplicate)
                {
                    results.Skipped++;
                }
                else
                {
                    results.Failed++;
                    results.Errors.Add($"{file.FileName}: {saveResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                results.Failed++;
                results.Errors.Add($"{file.FileName}: {ex.Message}");
            }
        }

        return Ok(results);
    }

    private async Task<SaveInvoiceResult> SaveInvoiceAsync(InvoiceConfirmDto invoiceDto)
    {
        try
        {
            // 1. Get or create supplier
            var supplier = await _supplierProductService.GetOrCreateSupplierAsync(invoiceDto.SupplierName);
            
            // 1.5. Get or create buyer (if TaxId provided)
            Guid? buyerId = null;
            if (!string.IsNullOrWhiteSpace(invoiceDto.BuyerTaxId))
            {
                try
                {
                    var buyer = await _supplierProductService.GetOrCreateBuyerAsync(
                        invoiceDto.BuyerTaxId,
                        invoiceDto.BuyerName
                    );
                    buyerId = buyer.Id;
                }
                catch (Exception ex)
                {
                    return new SaveInvoiceResult
                    {
                        IsSuccess = false,
                        IsDuplicate = false,
                        ErrorMessage = $"Villa við að búa til eða sækja kaupanda: {ex.Message}"
                    };
                }
            }
            
            // 2. Check for duplicate invoice (same supplier + invoice number)
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.SupplierId == supplier.Id && i.InvoiceNumber == invoiceDto.InvoiceNumber);
            
            if (existingInvoice != null)
            {
                return new SaveInvoiceResult
                {
                    IsSuccess = false,
                    IsDuplicate = true,
                    ErrorMessage = $"Reikningur með númeri {invoiceDto.InvoiceNumber} frá {invoiceDto.SupplierName} er þegar til í kerfinu."
                };
            }
            
            // 3. Create invoice entity
            var invoice = new Invoice
            {
                Id = invoiceDto.Id == Guid.Empty ? Guid.NewGuid() : invoiceDto.Id,
                SupplierId = supplier.Id,
                BuyerId = buyerId,
                SupplierName = invoiceDto.SupplierName,
                BuyerName = invoiceDto.BuyerName,
                BuyerTaxId = invoiceDto.BuyerTaxId,
                InvoiceNumber = invoiceDto.InvoiceNumber,
                InvoiceDate = invoiceDto.InvoiceDate.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(invoiceDto.InvoiceDate, DateTimeKind.Utc)
                    : invoiceDto.InvoiceDate.Kind == DateTimeKind.Local 
                        ? invoiceDto.InvoiceDate.ToUniversalTime()
                        : invoiceDto.InvoiceDate,
                TotalAmount = invoiceDto.TotalAmount,
                CreatedAt = DateTime.UtcNow
            };
            
            // 4. Process items: get or create products and create InvoiceItem entities
            foreach (var itemDto in invoiceDto.Items)
            {
                var product = await _supplierProductService.GetOrCreateProductAsync(
                    supplier.Id,
                    itemDto.ItemId,
                    itemDto.ItemName,
                    itemDto.Unit
                );
                
                var invoiceItem = new InvoiceItem
                {
                    Id = itemDto.Id == Guid.Empty ? Guid.NewGuid() : itemDto.Id,
                    InvoiceId = invoice.Id,
                    ProductId = product.Id,
                    ItemName = itemDto.ItemName,
                    ItemId = itemDto.ItemId,
                    Quantity = itemDto.Quantity,
                    Unit = itemDto.Unit,
                    UnitPrice = itemDto.UnitPrice,
                    ListPrice = itemDto.ListPrice,
                    VatCode = itemDto.VatCode,
                    Discount = itemDto.Discount,
                    TotalPrice = itemDto.TotalPrice,
                    TotalPriceWithVat = itemDto.TotalPriceWithVat
                };
                
                invoice.Items.Add(invoiceItem);
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return new SaveInvoiceResult
            {
                IsSuccess = true,
                IsDuplicate = false,
                ErrorMessage = null
            };
        }
        catch (Exception ex)
        {
            return new SaveInvoiceResult
            {
                IsSuccess = false,
                IsDuplicate = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private class SaveInvoiceResult
    {
        public bool IsSuccess { get; set; }
        public bool IsDuplicate { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class BulkUploadResult
    {
        public int Total { get; set; }
        public int Successful { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] Guid? buyerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? sortBy = "invoiceDate",
        [FromQuery] string? sortOrder = "desc")
    {
        var query = _context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.Buyer)
            .AsQueryable();

        // Apply filters
        if (supplierId.HasValue)
        {
            query = query.Where(i => i.SupplierId == supplierId.Value);
        }

        if (buyerId.HasValue)
        {
            query = query.Where(i => i.BuyerId == buyerId.Value);
        }

        if (startDate.HasValue)
        {
            var startDateUtc = startDate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
                : startDate.Value.Kind == DateTimeKind.Local
                    ? startDate.Value.ToUniversalTime()
                    : startDate.Value;
            query = query.Where(i => i.InvoiceDate >= startDateUtc);
        }

        if (endDate.HasValue)
        {
            var endDateUtc = endDate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc)
                : endDate.Value.Kind == DateTimeKind.Local
                    ? endDate.Value.ToUniversalTime()
                    : endDate.Value;
            // Include the entire end date
            endDateUtc = endDateUtc.Date.AddDays(1).AddTicks(-1);
            query = query.Where(i => i.InvoiceDate <= endDateUtc);
        }

        // Apply sorting
        var sortByLower = (sortBy ?? "invoiceDate").ToLower();
        var sortOrderLower = (sortOrder ?? "desc").ToLower();
        
        query = sortByLower switch
        {
            "invoicedate" => sortOrderLower == "asc"
                ? query.OrderBy(i => i.InvoiceDate)
                : query.OrderByDescending(i => i.InvoiceDate),
            "invoicenumber" => sortOrderLower == "asc"
                ? query.OrderBy(i => i.InvoiceNumber)
                : query.OrderByDescending(i => i.InvoiceNumber),
            "totalamount" => sortOrderLower == "asc"
                ? query.OrderBy(i => i.TotalAmount)
                : query.OrderByDescending(i => i.TotalAmount),
            "suppliername" => sortOrderLower == "asc"
                ? query.OrderBy(i => i.SupplierName)
                : query.OrderByDescending(i => i.SupplierName),
            "buyername" => sortOrderLower == "asc"
                ? query.OrderBy(i => i.BuyerName)
                : query.OrderByDescending(i => i.BuyerName),
            "createdat" => sortOrderLower == "asc"
                ? query.OrderBy(i => i.CreatedAt)
                : query.OrderByDescending(i => i.CreatedAt),
            _ => query.OrderByDescending(i => i.InvoiceDate) // Default: newest first
        };

        var invoices = await query
            .Select(i => new
            {
                i.Id,
                i.SupplierId,
                i.BuyerId,
                i.SupplierName,
                i.BuyerName,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.TotalAmount,
                i.CreatedAt,
                ItemCount = i.Items.Count,
                Supplier = new
                {
                    i.Supplier.Id,
                    i.Supplier.Name
                },
                Buyer = i.Buyer != null ? new
                {
                    i.Buyer.Id,
                    i.Buyer.Name
                } : null
            })
            .ToListAsync();

        return Ok(invoices);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Buyer)
            .FirstOrDefaultAsync(i => i.Id == id);
            
        if (invoice == null) return NotFound();
        
        // If BuyerTaxId is empty but we have a Buyer relationship, populate it from the buyer
        if (string.IsNullOrEmpty(invoice.BuyerTaxId) && invoice.Buyer != null)
        {
            invoice.BuyerTaxId = invoice.Buyer.TaxId;
        }
        
        return Ok(invoice);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvoice(Guid id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
        
        if (invoice == null)
            return NotFound();

        // Delete invoice (items will be cascade deleted due to foreign key configuration)
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
