using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WaitTimeRecordsController : ControllerBase
{
    private readonly AppDbContext _context;

    public WaitTimeRecordsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecords(
        [FromQuery] Restaurant? restaurant = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var query = _context.WaitTimeRecords.AsQueryable();

        if (restaurant.HasValue)
            query = query.Where(r => r.Restaurant == restaurant.Value);

        if (from.HasValue)
        {
            // Convert to UTC (query parameters come as Unspecified)
            var fromUtc = from.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc)
                : from.Value.Kind == DateTimeKind.Local
                    ? from.Value.ToUniversalTime()
                    : from.Value;
            query = query.Where(r => r.ScrapedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            // Convert to UTC (query parameters come as Unspecified)
            var toUtc = to.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc)
                : to.Value.Kind == DateTimeKind.Local
                    ? to.Value.ToUniversalTime()
                    : to.Value;
            // Include the entire end date
            toUtc = toUtc.Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.ScrapedAt <= toUtc);
        }

        var records = await query
            .OrderByDescending(r => r.ScrapedAt)
            .Select(r => new WaitTimeRecordDto
            {
                Id = r.Id,
                Restaurant = r.Restaurant,
                SottMinutes = r.SottMinutes,
                SentMinutes = r.SentMinutes,
                IsClosed = r.IsClosed,
                ScrapedAt = r.ScrapedAt
            })
            .ToListAsync();

        return Ok(records);
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestRecords()
    {
        var latestGreifinn = await _context.WaitTimeRecords
            .Where(r => r.Restaurant == Restaurant.Greifinn)
            .OrderByDescending(r => r.ScrapedAt)
            .FirstOrDefaultAsync();

        var latestSpretturinn = await _context.WaitTimeRecords
            .Where(r => r.Restaurant == Restaurant.Spretturinn)
            .OrderByDescending(r => r.ScrapedAt)
            .FirstOrDefaultAsync();

        var result = new List<WaitTimeRecordDto>();

        if (latestGreifinn != null)
        {
            result.Add(new WaitTimeRecordDto
            {
                Id = latestGreifinn.Id,
                Restaurant = latestGreifinn.Restaurant,
                SottMinutes = latestGreifinn.SottMinutes,
                SentMinutes = latestGreifinn.SentMinutes,
                IsClosed = latestGreifinn.IsClosed,
                ScrapedAt = latestGreifinn.ScrapedAt
            });
        }

        if (latestSpretturinn != null)
        {
            result.Add(new WaitTimeRecordDto
            {
                Id = latestSpretturinn.Id,
                Restaurant = latestSpretturinn.Restaurant,
                SottMinutes = latestSpretturinn.SottMinutes,
                SentMinutes = latestSpretturinn.SentMinutes,
                IsClosed = latestSpretturinn.IsClosed,
                ScrapedAt = latestSpretturinn.ScrapedAt
            });
        }

        return Ok(result);
    }
}
