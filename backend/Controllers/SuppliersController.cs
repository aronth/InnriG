using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SuppliersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _context.Suppliers
            .OrderBy(s => s.Name)
            .ToListAsync();
        
        return Ok(suppliers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSupplier(Guid id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Products)
            .Include(s => s.Invoices)
            .FirstOrDefaultAsync(s => s.Id == id);
        
        if (supplier == null) 
            return NotFound();
        
        return Ok(supplier);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] Supplier supplier)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        supplier.Id = Guid.NewGuid();
        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = DateTime.UtcNow;

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] Supplier supplier)
    {
        if (id != supplier.Id)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        supplier.UpdatedAt = DateTime.UtcNow;
        
        _context.Entry(supplier).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Suppliers.AnyAsync(s => s.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(Guid id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        
        if (supplier == null)
            return NotFound();

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
