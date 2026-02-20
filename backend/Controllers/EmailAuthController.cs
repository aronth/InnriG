using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/auth/email")]
[Authorize(Roles = "User,Manager,Admin")]
public class EmailAuthController : ControllerBase
{
    private readonly IMicrosoftOAuthService _oauthService;
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<EmailAuthController> _logger;

    public EmailAuthController(
        IMicrosoftOAuthService oauthService,
        AppDbContext context,
        UserManager<User> userManager,
        ILogger<EmailAuthController> logger)
    {
        _oauthService = oauthService;
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectEmail([FromBody] ConnectEmailRequestDto dto)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            // Validate email address
            if (string.IsNullOrWhiteSpace(dto.EmailAddress))
            {
                return BadRequest(new { error = "Email address is required" });
            }

            // Check if this is system inbox - only allow one system inbox
            if (dto.IsSystemInbox)
            {
                var existingSystemInbox = await _context.UserEmailTokens
                    .FirstOrDefaultAsync(t => t.IsSystemInbox);

                if (existingSystemInbox != null && existingSystemInbox.UserId != currentUser.Id)
                {
                    return BadRequest(new { error = "System inbox is already connected by another user" });
                }
            }

            // Check if email is already connected by this user
            var existingToken = await _context.UserEmailTokens
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id && t.EmailAddress == dto.EmailAddress);

            if (existingToken != null)
            {
                return BadRequest(new { error = "Email address is already connected" });
            }

            // Initiate device code flow
            var connectionInfo = await _oauthService.InitiateDeviceCodeFlowAsync(
                dto.EmailAddress, 
                dto.IsSystemInbox);

            return Ok(connectionInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating email connection");
            return StatusCode(500, new { error = "Error initiating email connection", details = ex.Message });
        }
    }

    [HttpPost("poll")]
    public async Task<IActionResult> PollForToken([FromBody] PollTokenRequestDto dto)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            // Poll for token (this will wait until user authenticates or timeout)
            var token = await _oauthService.PollForTokenAsync(
                currentUser.Id, 
                dto.DeviceCode, 
                5, // Default interval
                HttpContext.RequestAborted);

            if (token == null)
            {
                return BadRequest(new { error = "Failed to obtain token. Please try again." });
            }

            // Update IsSystemInbox flag if needed
            // This is set during device code flow, but we ensure it's correct here
            var existingToken = await _context.UserEmailTokens
                .FirstOrDefaultAsync(t => t.Id == token.Id);

            if (existingToken != null)
            {
                // Check if this should be system inbox
                var shouldBeSystemInbox = await ShouldBeSystemInboxAsync(token.EmailAddress);
                if (shouldBeSystemInbox && !existingToken.IsSystemInbox)
                {
                    // Unset other system inbox tokens
                    var otherSystemInbox = await _context.UserEmailTokens
                        .Where(t => t.IsSystemInbox && t.Id != token.Id)
                        .ToListAsync();
                    foreach (var other in otherSystemInbox)
                    {
                        other.IsSystemInbox = false;
                    }

                    existingToken.IsSystemInbox = true;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new 
            { 
                success = true, 
                emailAddress = token.EmailAddress,
                isSystemInbox = token.IsSystemInbox
            });
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Token polling timed out");
            return BadRequest(new { error = "Authentication timed out. Please try again." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Token polling failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling for token");
            return StatusCode(500, new { error = "Error polling for token", details = ex.Message });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetConnectionStatus()
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var status = await _oauthService.GetConnectionStatusAsync(currentUser.Id);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection status");
            return StatusCode(500, new { error = "Error getting connection status", details = ex.Message });
        }
    }

    [HttpDelete("disconnect/{emailAddress}")]
    public async Task<IActionResult> DisconnectEmail(string emailAddress)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var success = await _oauthService.RevokeTokenAsync(currentUser.Id, emailAddress);

            if (!success)
            {
                return NotFound(new { error = "Email connection not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting email");
            return StatusCode(500, new { error = "Error disconnecting email", details = ex.Message });
        }
    }

    [HttpPut("set-system-inbox")]
    public async Task<IActionResult> SetSystemInbox([FromBody] SetSystemInboxDto dto)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            // Find the token for this user and email
            var token = await _context.UserEmailTokens
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id && t.EmailAddress == dto.EmailAddress);

            if (token == null)
            {
                return NotFound(new { error = "Email connection not found" });
            }

            // If setting as system inbox, unset other system inbox tokens first
            if (dto.IsSystemInbox)
            {
                var otherSystemInbox = await _context.UserEmailTokens
                    .Where(t => t.IsSystemInbox && t.Id != token.Id)
                    .ToListAsync();
                
                foreach (var other in otherSystemInbox)
                {
                    other.IsSystemInbox = false;
                    _logger.LogInformation("Unsetting system inbox flag for email {EmailAddress} (new system inbox: {NewEmail})", 
                        other.EmailAddress, dto.EmailAddress);
                }
            }

            token.IsSystemInbox = dto.IsSystemInbox;
            token.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("System inbox flag updated for email {EmailAddress}: {IsSystemInbox}", 
                dto.EmailAddress, dto.IsSystemInbox);

            return Ok(new { message = "System inbox flag updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting system inbox for email {EmailAddress}", dto.EmailAddress);
            return StatusCode(500, new { error = "Error setting system inbox", details = ex.Message });
        }
    }

    private async Task<bool> ShouldBeSystemInboxAsync(string emailAddress)
    {
        // Check if this email matches the configured system inbox email
        var configuredInbox = _context.UserEmailTokens
            .FirstOrDefault(t => t.IsSystemInbox);

        // If no system inbox exists, and this matches the configured email, make it system inbox
        if (configuredInbox == null)
        {
            // We can't easily check configuration here without injecting IConfiguration
            // For now, we'll rely on the IsSystemInbox flag being set during connection
            return false;
        }

        return configuredInbox.EmailAddress == emailAddress;
    }
}

public class SetSystemInboxDto
{
    public string EmailAddress { get; set; } = string.Empty;
    public bool IsSystemInbox { get; set; }
}


