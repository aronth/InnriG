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
[Route("api/[controller]")]
[Authorize]
public class WaitTimeNotificationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IPushoverService _pushoverService;

    public WaitTimeNotificationsController(
        AppDbContext context,
        UserManager<User> userManager,
        IPushoverService pushoverService)
    {
        _context = context;
        _userManager = userManager;
        _pushoverService = pushoverService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var notifications = await _context.WaitTimeNotifications
            .Where(n => n.UserId == currentUser.Id)
            .Select(n => new WaitTimeNotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Restaurant = n.Restaurant,
                SottThresholdMinutes = n.SottThresholdMinutes,
                SentThresholdMinutes = n.SentThresholdMinutes,
                IsEnabled = n.IsEnabled,
                LastNotifiedSott = n.LastNotifiedSott,
                LastNotifiedSent = n.LastNotifiedSent,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(Guid id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var notification = await _context.WaitTimeNotifications
            .Where(n => n.Id == id && n.UserId == currentUser.Id)
            .Select(n => new WaitTimeNotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Restaurant = n.Restaurant,
                SottThresholdMinutes = n.SottThresholdMinutes,
                SentThresholdMinutes = n.SentThresholdMinutes,
                IsEnabled = n.IsEnabled,
                LastNotifiedSott = n.LastNotifiedSott,
                LastNotifiedSent = n.LastNotifiedSent,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (notification == null)
            return NotFound();

        return Ok(notification);
    }

    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateWaitTimeNotificationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        // Validate Pushover credentials
        var isValid = await _pushoverService.ValidateCredentialsAsync(dto.PushoverUserKey);
        if (!isValid)
            return BadRequest("Invalid Pushover user key. Please check your user key.");

        // Check if notification already exists for this user and restaurant
        var existing = await _context.WaitTimeNotifications
            .FirstOrDefaultAsync(n => n.UserId == currentUser.Id && n.Restaurant == dto.Restaurant);

        if (existing != null)
            return BadRequest("Notification settings already exist for this restaurant. Use PUT to update.");

        var notification = new WaitTimeNotification
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Restaurant = dto.Restaurant,
            PushoverUserKey = dto.PushoverUserKey,
            SottThresholdMinutes = dto.SottThresholdMinutes,
            SentThresholdMinutes = dto.SentThresholdMinutes,
            IsEnabled = dto.IsEnabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WaitTimeNotifications.Add(notification);
        await _context.SaveChangesAsync();

        var result = new WaitTimeNotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Restaurant = notification.Restaurant,
            SottThresholdMinutes = notification.SottThresholdMinutes,
            SentThresholdMinutes = notification.SentThresholdMinutes,
            IsEnabled = notification.IsEnabled,
            LastNotifiedSott = notification.LastNotifiedSott,
            LastNotifiedSent = notification.LastNotifiedSent,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };

        return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNotification(Guid id, [FromBody] UpdateWaitTimeNotificationDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var notification = await _context.WaitTimeNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);

        if (notification == null)
            return NotFound();

        // Validate Pushover credentials if user key is provided
        if (!string.IsNullOrEmpty(dto.PushoverUserKey))
        {
            var isValid = await _pushoverService.ValidateCredentialsAsync(dto.PushoverUserKey);
            if (!isValid)
                return BadRequest("Invalid Pushover user key. Please check your user key.");
        }

        // Update fields
        if (!string.IsNullOrEmpty(dto.PushoverUserKey))
            notification.PushoverUserKey = dto.PushoverUserKey;
        
        if (dto.SottThresholdMinutes.HasValue)
            notification.SottThresholdMinutes = dto.SottThresholdMinutes;
        
        if (dto.SentThresholdMinutes.HasValue)
            notification.SentThresholdMinutes = dto.SentThresholdMinutes;
        
        if (dto.IsEnabled.HasValue)
            notification.IsEnabled = dto.IsEnabled.Value;

        notification.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var result = new WaitTimeNotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Restaurant = notification.Restaurant,
            SottThresholdMinutes = notification.SottThresholdMinutes,
            SentThresholdMinutes = notification.SentThresholdMinutes,
            IsEnabled = notification.IsEnabled,
            LastNotifiedSott = notification.LastNotifiedSott,
            LastNotifiedSent = notification.LastNotifiedSent,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var notification = await _context.WaitTimeNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);

        if (notification == null)
            return NotFound();

        _context.WaitTimeNotifications.Remove(notification);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
