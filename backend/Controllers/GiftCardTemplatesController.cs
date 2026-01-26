using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Manager,Admin")]
public class GiftCardTemplatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public GiftCardTemplatesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates([FromQuery] bool? isActive = null)
    {
        var query = _context.GiftCardTemplates.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var templates = await query
            .Include(t => t.Restaurant)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var dtos = templates.Select(t => MapToDto(t)).ToList();
        
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplate(Guid id)
    {
        var template = await _context.GiftCardTemplates
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (template == null)
            return NotFound();

        return Ok(MapToDto(template));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateGiftCardTemplateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var template = new GiftCardTemplate
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            DefaultAmount = dto.DefaultAmount,
            MessageTemplate = dto.MessageTemplate,
            AmountAsText = dto.AmountAsText,
            IsMonetaryTemplate = dto.IsMonetaryTemplate,
            RestaurantId = dto.RestaurantId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GiftCardTemplates.Add(template);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, MapToDto(template));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateGiftCardTemplateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var template = await _context.GiftCardTemplates
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (template == null)
            return NotFound();

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.DefaultAmount = dto.DefaultAmount;
        template.MessageTemplate = dto.MessageTemplate;
        template.AmountAsText = dto.AmountAsText;
        template.IsMonetaryTemplate = dto.IsMonetaryTemplate;
        template.RestaurantId = dto.RestaurantId;
        template.IsActive = dto.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapToDto(template));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var template = await _context.GiftCardTemplates
            .Include(t => t.GiftCards)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (template == null)
            return NotFound();

        // Check if template is used by any gift cards
        if (template.GiftCards.Any())
        {
            return BadRequest("Cannot delete template that is used by gift cards. Deactivate it instead.");
        }

        _context.GiftCardTemplates.Remove(template);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private GiftCardTemplateDto MapToDto(GiftCardTemplate template)
    {
        return new GiftCardTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            DefaultAmount = template.DefaultAmount,
            MessageTemplate = template.MessageTemplate,
            AmountAsText = template.AmountAsText,
            IsMonetaryTemplate = template.IsMonetaryTemplate,
            RestaurantId = template.RestaurantId,
            RestaurantName = template.Restaurant?.Name,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}



