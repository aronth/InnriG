using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/email-classifications")]
// [Authorize(Roles = "SystemAdmin,Admin")] // Temporarily disabled
public class EmailClassificationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailClassificationsController> _logger;

    public EmailClassificationsController(
        AppDbContext context,
        ILogger<EmailClassificationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmailClassificationDto>>> GetAll(
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isSystem = null)
    {
        var query = _context.EmailClassifications.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (isSystem.HasValue)
        {
            query = query.Where(c => c.IsSystem == isSystem.Value);
        }

        var classifications = await query
            .OrderBy(c => c.IsSystem ? 0 : 1)
            .ThenBy(c => c.Name)
            .Select(c => new EmailClassificationDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                SystemPrompt = c.SystemPrompt,
                IsSystem = c.IsSystem,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(classifications);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<EmailClassificationDto>>> GetActive()
    {
        var classifications = await _context.EmailClassifications
            .Where(c => c.IsActive)
            .OrderBy(c => c.IsSystem ? 0 : 1)
            .ThenBy(c => c.Name)
            .Select(c => new EmailClassificationDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                SystemPrompt = c.SystemPrompt,
                IsSystem = c.IsSystem,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(classifications);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmailClassificationDto>> GetById(Guid id)
    {
        var classification = await _context.EmailClassifications
            .FirstOrDefaultAsync(c => c.Id == id);

        if (classification == null)
        {
            return NotFound();
        }

        return Ok(new EmailClassificationDto
        {
            Id = classification.Id,
            Name = classification.Name,
            Description = classification.Description,
            SystemPrompt = classification.SystemPrompt,
            IsSystem = classification.IsSystem,
            IsActive = classification.IsActive,
            CreatedAt = classification.CreatedAt,
            UpdatedAt = classification.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<EmailClassificationDto>> Create([FromBody] CreateEmailClassificationDto dto)
    {
        // Validate name is unique
        var nameExists = await _context.EmailClassifications
            .AnyAsync(c => c.Name == dto.Name);

        if (nameExists)
        {
            return BadRequest(new { error = $"Classification with name '{dto.Name}' already exists" });
        }

        var classification = new EmailClassification
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            SystemPrompt = dto.SystemPrompt,
            IsSystem = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmailClassifications.Add(classification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created email classification {ClassificationName} with ID {ClassificationId}", classification.Name, classification.Id);

        return CreatedAtAction(nameof(GetById), new { id = classification.Id }, new EmailClassificationDto
        {
            Id = classification.Id,
            Name = classification.Name,
            Description = classification.Description,
            SystemPrompt = classification.SystemPrompt,
            IsSystem = classification.IsSystem,
            IsActive = classification.IsActive,
            CreatedAt = classification.CreatedAt,
            UpdatedAt = classification.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EmailClassificationDto>> Update(Guid id, [FromBody] UpdateEmailClassificationDto dto)
    {
        var classification = await _context.EmailClassifications
            .FirstOrDefaultAsync(c => c.Id == id);

        if (classification == null)
        {
            return NotFound();
        }

        // Validate name is unique (if changed)
        if (classification.Name != dto.Name)
        {
            var nameExists = await _context.EmailClassifications
                .AnyAsync(c => c.Name == dto.Name && c.Id != id);

            if (nameExists)
            {
                return BadRequest(new { error = $"Classification with name '{dto.Name}' already exists" });
            }
        }

        classification.Name = dto.Name;
        classification.Description = dto.Description;
        classification.SystemPrompt = dto.SystemPrompt;
        classification.IsActive = dto.IsActive;
        classification.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated email classification {ClassificationName} with ID {ClassificationId}", classification.Name, classification.Id);

        return Ok(new EmailClassificationDto
        {
            Id = classification.Id,
            Name = classification.Name,
            Description = classification.Description,
            SystemPrompt = classification.SystemPrompt,
            IsSystem = classification.IsSystem,
            IsActive = classification.IsActive,
            CreatedAt = classification.CreatedAt,
            UpdatedAt = classification.UpdatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var classification = await _context.EmailClassifications
            .Include(c => c.Workflows)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (classification == null)
        {
            return NotFound();
        }

        // Check if classification has workflows
        if (classification.Workflows.Any())
        {
            // Soft delete
            classification.IsActive = false;
            classification.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Soft deleted email classification {ClassificationName} with ID {ClassificationId}", classification.Name, classification.Id);
        }
        else
        {
            // Hard delete if no workflows
            _context.EmailClassifications.Remove(classification);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted email classification {ClassificationName} with ID {ClassificationId}", classification.Name, classification.Id);
        }

        return NoContent();
    }
}

