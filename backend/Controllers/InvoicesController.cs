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
            // Return for review, do not save yet
            return Ok(invoice);
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

        // 1. Get or create supplier
        var supplier = await _supplierProductService.GetOrCreateSupplierAsync(invoiceDto.SupplierName);
        
        // 2. Create invoice entity
        var invoice = new Invoice
        {
            Id = invoiceDto.Id == Guid.Empty ? Guid.NewGuid() : invoiceDto.Id,
            SupplierId = supplier.Id,
            SupplierName = invoiceDto.SupplierName,
            InvoiceNumber = invoiceDto.InvoiceNumber,
            InvoiceDate = invoiceDto.InvoiceDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(invoiceDto.InvoiceDate, DateTimeKind.Utc)
                : invoiceDto.InvoiceDate.Kind == DateTimeKind.Local 
                    ? invoiceDto.InvoiceDate.ToUniversalTime()
                    : invoiceDto.InvoiceDate,
            TotalAmount = invoiceDto.TotalAmount,
            CreatedAt = DateTime.UtcNow
        };
        
        // 3. Process items: get or create products and create InvoiceItem entities
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

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
            
        if (invoice == null) return NotFound();
        
        return Ok(invoice);
    }
}
