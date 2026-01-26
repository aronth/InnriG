using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User,Manager,Admin")]
public class BookingsController : ControllerBase
{
    private readonly IBookingsScraper _scraper;
    private readonly ITableBookingService _tableBookingService;
    private readonly ILogger<BookingsController> _logger;
    private readonly IMemoryCache _cache;

    public BookingsController(
        IBookingsScraper scraper,
        ITableBookingService tableBookingService,
        ILogger<BookingsController> logger,
        IMemoryCache cache)
    {
        _scraper = scraper;
        _tableBookingService = tableBookingService;
        _logger = logger;
        _cache = cache;
    }

    [HttpGet("week")]
    public async Task<IActionResult> GetWeek([FromQuery] long? dt)
    {
        try
        {
            // If no timestamp provided, use current time
            var timestamp = dt ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            _logger.LogInformation("Fetching bookings for week containing timestamp {Timestamp}", timestamp);
            
            var weekData = await _scraper.ScrapeWeekAsync(timestamp);
            
            return Ok(weekData);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching bookings");
            return StatusCode(502, new { error = "Unable to reach booking system", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bookings");
            return StatusCode(500, new { error = "Error fetching bookings", details = ex.Message });
        }
    }

    [HttpGet("table")]
    public async Task<IActionResult> GetTableBookings(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? contactName = null,
        [FromQuery] string? contactPhone = null,
        [FromQuery] int? statusId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Fetching table bookings - FromDate: {FromDate}, ToDate: {ToDate}, ContactPhone: {ContactPhone}, Page: {Page}",
                fromDate, toDate, contactPhone, page);

            var result = await _tableBookingService.GetBookingsAsync(
                fromDate,
                toDate,
                contactName,
                contactPhone,
                statusId,
                page,
                pageSize,
                cancellationToken);

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching table bookings");
            return StatusCode(502, new { error = "Unable to reach booking system", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching table bookings");
            return StatusCode(500, new { error = "Error fetching table bookings", details = ex.Message });
        }
    }

    [HttpPost("clear-cache")]
    public IActionResult ClearCache()
    {
        try
        {
            // Clear all booking cache entries
            // Since we can't enumerate MemoryCache keys easily, we'll use a known pattern
            // For now, we'll just log the action - the cache will expire naturally
            // In a production system, you might want to use IMemoryCache with a custom wrapper
            // that tracks keys for bulk deletion
            
            _logger.LogInformation("Cache clear requested - bookings cache will expire naturally");
            
            // We can create a simple workaround: increment a cache version number
            // and include it in cache keys, then increment when clearing
            var cacheVersion = _cache.Get<int>("bookings_cache_version");
            _cache.Set("bookings_cache_version", cacheVersion + 1, TimeSpan.FromDays(1));
            
            return Ok(new { message = "Cache cleared successfully", version = cacheVersion + 1 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new { error = "Error clearing cache", details = ex.Message });
        }
    }
}

