using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/user-email-settings")]
[Authorize(Roles = "User,Manager,Admin")]
public class UserEmailSettingsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserEmailSettingsController> _logger;

    public UserEmailSettingsController(
        AppDbContext context,
        UserManager<User> userManager,
        ILogger<UserEmailSettingsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("email-mappings")]
    public async Task<IActionResult> GetEmailMappings()
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var mappings = await _context.UserEmailMappings
                .Where(m => m.UserId == currentUser.Id)
                .OrderBy(m => m.IsDefault ? 0 : 1)
                .ThenBy(m => m.CreatedAt)
                .Select(m => new UserEmailMappingDto
                {
                    Id = m.Id,
                    EmailAddress = m.EmailAddress,
                    DisplayName = m.DisplayName,
                    IsDefault = m.IsDefault,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email mappings");
            return StatusCode(500, new { error = "Error getting email mappings", details = ex.Message });
        }
    }

    [HttpPost("email-mappings")]
    public async Task<IActionResult> CreateEmailMapping([FromBody] CreateUserEmailMappingDto dto)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            // Check if email already exists for this user
            var existing = await _context.UserEmailMappings
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id && m.EmailAddress == dto.EmailAddress);

            if (existing != null)
                return BadRequest(new { error = "Email address already linked to your account" });

            // If this is set as default, unset other defaults
            if (dto.IsDefault)
            {
                var otherDefaults = await _context.UserEmailMappings
                    .Where(m => m.UserId == currentUser.Id && m.IsDefault)
                    .ToListAsync();
                foreach (var otherMapping in otherDefaults)
                {
                    otherMapping.IsDefault = false;
                    otherMapping.UpdatedAt = DateTime.UtcNow;
                }
            }

            var mapping = new UserEmailMapping
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.Id,
                EmailAddress = dto.EmailAddress,
                DisplayName = dto.DisplayName,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserEmailMappings.Add(mapping);
            await _context.SaveChangesAsync();

            var result = new UserEmailMappingDto
            {
                Id = mapping.Id,
                EmailAddress = mapping.EmailAddress,
                DisplayName = mapping.DisplayName,
                IsDefault = mapping.IsDefault,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            };

            return CreatedAtAction(nameof(GetEmailMappings), new { }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email mapping");
            return StatusCode(500, new { error = "Error creating email mapping", details = ex.Message });
        }
    }

    [HttpPut("email-mappings/{id}")]
    public async Task<IActionResult> UpdateEmailMapping(Guid id, [FromBody] UpdateUserEmailMappingDto dto)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var mapping = await _context.UserEmailMappings
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);

            if (mapping == null)
                return NotFound();

            // If setting as default, unset other defaults
            if (dto.IsDefault && !mapping.IsDefault)
            {
                var otherDefaults = await _context.UserEmailMappings
                    .Where(m => m.UserId == currentUser.Id && m.IsDefault && m.Id != id)
                    .ToListAsync();
                foreach (var otherMapping in otherDefaults)
                {
                    otherMapping.IsDefault = false;
                    otherMapping.UpdatedAt = DateTime.UtcNow;
                }
            }

            mapping.DisplayName = dto.DisplayName;
            mapping.IsDefault = dto.IsDefault;
            mapping.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var result = new UserEmailMappingDto
            {
                Id = mapping.Id,
                EmailAddress = mapping.EmailAddress,
                DisplayName = mapping.DisplayName,
                IsDefault = mapping.IsDefault,
                CreatedAt = mapping.CreatedAt,
                UpdatedAt = mapping.UpdatedAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email mapping {Id}", id);
            return StatusCode(500, new { error = "Error updating email mapping", details = ex.Message });
        }
    }

    [HttpDelete("email-mappings/{id}")]
    public async Task<IActionResult> DeleteEmailMapping(Guid id)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var mapping = await _context.UserEmailMappings
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == currentUser.Id);

            if (mapping == null)
                return NotFound();

            _context.UserEmailMappings.Remove(mapping);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email mapping {Id}", id);
            return StatusCode(500, new { error = "Error deleting email mapping", details = ex.Message });
        }
    }

    [HttpGet("signature")]
    public async Task<IActionResult> GetEmailSignature()
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            return Ok(new { signature = currentUser.EmailSignature ?? string.Empty });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email signature");
            return StatusCode(500, new { error = "Error getting email signature", details = ex.Message });
        }
    }

    [HttpPut("signature")]
    public async Task<IActionResult> UpdateEmailSignature([FromBody] UpdateEmailSignatureDto dto)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            currentUser.EmailSignature = dto.Signature;
            currentUser.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(currentUser);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            return Ok(new { signature = currentUser.EmailSignature ?? string.Empty });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email signature");
            return StatusCode(500, new { error = "Error updating email signature", details = ex.Message });
        }
    }
}

public class CreateUserEmailMappingDto
{
    public string EmailAddress { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdateUserEmailMappingDto
{
    public string? DisplayName { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdateEmailSignatureDto
{
    public string? Signature { get; set; }
}

