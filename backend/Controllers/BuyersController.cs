using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuyersController : ControllerBase
{
    private readonly AppDbContext _context;

    public BuyersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetBuyers()
    {
        var buyers = await _context.Buyers
            .OrderBy(b => b.Name)
            .ToListAsync();
        
        return Ok(buyers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBuyer(Guid id)
    {
        var buyer = await _context.Buyers
            .Include(b => b.Invoices)
                .ThenInclude(i => i.Supplier)
            .FirstOrDefaultAsync(b => b.Id == id);
        
        if (buyer == null) 
            return NotFound();
        
        return Ok(buyer);
    }
}

