using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/emails/junk-filters")]
[Authorize(Roles = "Admin,Manager")]
public class EmailJunkFilterController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailJunkFilterController> _logger;

    public EmailJunkFilterController(
        AppDbContext context,
        ILogger<EmailJunkFilterController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetJunkFilters()
    {
        try
        {
            var filters = await _context.EmailJunkFilters
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var dtos = filters.Select(f => MapToDto(f)).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting junk filters");
            return StatusCode(500, new { error = "Error getting junk filters", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJunkFilter(Guid id)
    {
        try
        {
            var filter = await _context.EmailJunkFilters.FindAsync(id);
            if (filter == null)
                return NotFound();

            return Ok(MapToDto(filter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting junk filter {Id}", id);
            return StatusCode(500, new { error = "Error getting junk filter", details = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateJunkFilter([FromBody] CreateEmailJunkFilterDto dto)
    {
        try
        {
            // Validate that at least one field is provided
            if (string.IsNullOrWhiteSpace(dto.Subject) && string.IsNullOrWhiteSpace(dto.SenderEmail))
            {
                return BadRequest(new { error = "Either Subject or SenderEmail must be provided" });
            }

            var filter = new EmailJunkFilter
            {
                Id = Guid.NewGuid(),
                Subject = string.IsNullOrWhiteSpace(dto.Subject) ? null : dto.Subject.Trim(),
                SenderEmail = string.IsNullOrWhiteSpace(dto.SenderEmail) ? null : dto.SenderEmail.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.EmailJunkFilters.Add(filter);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created junk filter {FilterId}: Subject='{Subject}', Sender='{Sender}'", 
                filter.Id, filter.Subject ?? "*", filter.SenderEmail ?? "*");

            return CreatedAtAction(nameof(GetJunkFilter), new { id = filter.Id }, MapToDto(filter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating junk filter");
            return StatusCode(500, new { error = "Error creating junk filter", details = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJunkFilter(Guid id, [FromBody] UpdateEmailJunkFilterDto dto)
    {
        try
        {
            var filter = await _context.EmailJunkFilters.FindAsync(id);
            if (filter == null)
                return NotFound();

            // Validate that at least one field is provided
            if (string.IsNullOrWhiteSpace(dto.Subject) && string.IsNullOrWhiteSpace(dto.SenderEmail))
            {
                return BadRequest(new { error = "Either Subject or SenderEmail must be provided" });
            }

            filter.Subject = string.IsNullOrWhiteSpace(dto.Subject) ? null : dto.Subject.Trim();
            filter.SenderEmail = string.IsNullOrWhiteSpace(dto.SenderEmail) ? null : dto.SenderEmail.Trim();
            filter.IsActive = dto.IsActive;
            filter.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated junk filter {FilterId}: Subject='{Subject}', Sender='{Sender}', IsActive={IsActive}", 
                filter.Id, filter.Subject ?? "*", filter.SenderEmail ?? "*", filter.IsActive);

            return Ok(MapToDto(filter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating junk filter {Id}", id);
            return StatusCode(500, new { error = "Error updating junk filter", details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJunkFilter(Guid id)
    {
        try
        {
            var filter = await _context.EmailJunkFilters.FindAsync(id);
            if (filter == null)
                return NotFound();

            _context.EmailJunkFilters.Remove(filter);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted junk filter {FilterId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting junk filter {Id}", id);
            return StatusCode(500, new { error = "Error deleting junk filter", details = ex.Message });
        }
    }

    private static EmailJunkFilterDto MapToDto(EmailJunkFilter filter)
    {
        return new EmailJunkFilterDto
        {
            Id = filter.Id,
            Subject = filter.Subject,
            SenderEmail = filter.SenderEmail,
            IsActive = filter.IsActive,
            CreatedAt = filter.CreatedAt,
            UpdatedAt = filter.UpdatedAt
        };
    }
}

