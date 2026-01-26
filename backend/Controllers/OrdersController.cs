using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IOrderImportService _importService;
    private readonly OrderLateRules _orderLateRules;
    private readonly IGreifinnOrderScraper _greifinnOrderScraper;

    public OrdersController(
        AppDbContext context, 
        IOrderImportService importService, 
        OrderLateRules orderLateRules,
        IGreifinnOrderScraper greifinnOrderScraper)
    {
        _context = context;
        _importService = importService;
        _orderLateRules = orderLateRules;
        _greifinnOrderScraper = greifinnOrderScraper;
    }

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches(CancellationToken cancellationToken)
    {
        var batches = await _context.OrderImportBatches
            .AsNoTracking()
            .OrderByDescending(b => b.ImportedAt)
            .ToListAsync(cancellationToken);

        return Ok(batches);
    }

    [HttpDelete("batches/{id:guid}")]
    public async Task<IActionResult> DeleteBatch(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _context.OrderImportBatches
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (batch == null)
            return NotFound();

        // Cascade delete will remove OrderRows for this batch.
        _context.OrderImportBatches.Remove(batch);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("rows")]
    public async Task<IActionResult> GetRows(
        [FromQuery] Guid? batchId,
        [FromQuery] Guid? restaurantId,
        [FromQuery] string? deliveryMethod,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool? isLate,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200,
        [FromQuery] bool includeTotal = false,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 1000);
        skip = Math.Max(0, skip);

        var query = _context.OrderRows.AsNoTracking().AsQueryable();
        
        if (batchId != null)
            query = query.Where(r => r.OrderImportBatchId == batchId.Value);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (fromDate != null)
        {
            var fromUtc = fromDate.Value.ToUniversalTime();
            query = query.Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc);
        }

        if (toDate != null)
        {
            var toUtc = toDate.Value.ToUniversalTime().Date.AddDays(1).AddTicks(-1); // end of day
            query = query.Where(r => r.DeliveryDate != null && r.DeliveryDate <= toUtc);
        }

        int totalCount = 0;
        List<Models.OrderRow> rows;

        if (isLate.HasValue)
        {
            // We need to evaluate late status in memory after fetching, or use a more complex query
            // For now, fetch the data and filter in memory for late status
            var allRows = await query
                .OrderByDescending(r => r.DeliveryDate ?? r.CreatedDate ?? r.CreatedAt)
                .ToListAsync(cancellationToken);

            var filtered = allRows.Where(r =>
            {
                if (r.DeliveryDate == null) return false;
                var late = _orderLateRules.IsLate(r.DeliveryMethod, r.DeliveryDate.Value, r.CheckedOutAt, r.ScannedAt, r.ReadyTime);
                return late == isLate.Value;
            }).ToList();

            totalCount = filtered.Count;
            rows = filtered.Skip(skip).Take(take).ToList();

            if (includeTotal)
            {
                return Ok(new PaginatedOrderRowsDto
                {
                    Items = rows,
                    TotalCount = totalCount,
                    Skip = skip,
                    Take = take
                });
            }

            return Ok(rows);
        }

        if (includeTotal)
        {
            totalCount = await query.CountAsync(cancellationToken);
        }

        rows = await query
            .OrderByDescending(r => r.DeliveryDate ?? r.CreatedDate ?? r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (includeTotal)
        {
            return Ok(new PaginatedOrderRowsDto
            {
                Items = rows,
                TotalCount = totalCount,
                Skip = skip,
                Take = take
            });
        }

        return Ok(rows);
    }

    [HttpPut("rows/{id:guid}/ready-time")]
    public async Task<IActionResult> UpdateReadyTime(
        Guid id,
        [FromBody] UpdateReadyTimeDto dto,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.OrderRows
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (order == null)
            return NotFound();

        order.ReadyTime = dto.ReadyTime;
        order.UpdatedAt = DateTime.UtcNow;

        // Recalculate wait time if we have both OrderTime and ReadyTime
        if (order.ReadyTime.HasValue && order.OrderTime.HasValue && order.DeliveryDate.HasValue)
        {
            var deliveryDate = order.DeliveryDate.Value;
            var readyDateTime = new DateTime(
                deliveryDate.Year,
                deliveryDate.Month,
                deliveryDate.Day,
                order.ReadyTime.Value.Hour,
                order.ReadyTime.Value.Minute,
                0,
                DateTimeKind.Utc);

            var orderDateTime = new DateTime(
                deliveryDate.Year,
                deliveryDate.Month,
                deliveryDate.Day,
                order.OrderTime.Value.Hour,
                order.OrderTime.Value.Minute,
                0,
                DateTimeKind.Utc);

            var waitTimeMinutes = (readyDateTime - orderDateTime).TotalMinutes;
            order.WaitTimeMin = (int)Math.Round(waitTimeMinutes / 5.0) * 5; // Round to nearest 5 minutes
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(order);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<OrderImportResultDto>> Upload(
        IFormFile file,
        [FromQuery] Guid? restaurantId,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        try
        {
            var result = await _importService.ImportAsync(file, restaurantId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error importing orders: {ex.Message}");
        }
    }

    [HttpGet("remaining-time-aggregations")]
    public async Task<IActionResult> GetRemainingTimeAggregations(
        [FromQuery] Guid? restaurantId,
        [FromQuery] string? deliveryMethod,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OrderRows.AsNoTracking().AsQueryable();

        // Filter by restaurant
        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        // Filter by delivery method
        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        // Filter by date range (on delivery date)
        if (fromDate != null)
        {
            var fromUtc = fromDate.Value.ToUniversalTime();
            query = query.Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc);
        }

        if (toDate != null)
        {
            var toUtc = toDate.Value.ToUniversalTime().Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.DeliveryDate != null && r.DeliveryDate <= toUtc);
        }

        // Exclude "Salur" orders (remaining time not applicable)
        query = query.Where(r => r.DeliveryMethod != "Salur");

        // Fetch orders with required fields
        var orders = await query
            .Where(r => r.DeliveryDate != null && r.ReadyTime != null)
            .Where(r => (r.DeliveryMethod == "Sótt" && r.ScannedAt != null) || 
                       (r.DeliveryMethod == "Sent" && r.CheckedOutAt != null))
            .Select(r => new
            {
                r.DeliveryDate,
                r.ReadyTime,
                r.DeliveryMethod,
                r.ScannedAt,
                r.CheckedOutAt
            })
            .ToListAsync(cancellationToken);

        // Calculate remaining time for each order and group by day and 15-minute intervals
        var results = new List<RemainingTimeAggregationDto>();

        var ordersByDate = orders
            .Select(o =>
            {
                var deliveryDate = o.DeliveryDate!.Value;
                var readyTime = o.ReadyTime!.Value;
                
                // Combine delivery date with ready time
                var readyDateTime = new DateTime(
                    deliveryDate.Year,
                    deliveryDate.Month,
                    deliveryDate.Day,
                    readyTime.Hour,
                    readyTime.Minute,
                    0,
                    DateTimeKind.Utc);

                // Determine action time based on delivery method
                DateTime? actionDateTime = null;
                if (o.DeliveryMethod == "Sótt" && o.ScannedAt != null)
                {
                    actionDateTime = o.ScannedAt.Value;
                }
                else if (o.DeliveryMethod == "Sent" && o.CheckedOutAt != null)
                {
                    actionDateTime = o.CheckedOutAt.Value;
                }

                if (!actionDateTime.HasValue)
                    return null;

                // Calculate remaining time in minutes
                var remainingMinutes = (readyDateTime - actionDateTime.Value).TotalMinutes;

                // Round action time to nearest 15-minute interval (for grouping)
                var actionTime = actionDateTime.Value;
                var roundedMinutes = (actionTime.Minute / 15) * 15;
                var intervalStart = new DateTime(
                    actionTime.Year,
                    actionTime.Month,
                    actionTime.Day,
                    actionTime.Hour,
                    roundedMinutes,
                    0,
                    DateTimeKind.Utc);

                return new
                {
                    Date = DateOnly.FromDateTime(actionTime),
                    IntervalStart = intervalStart,
                    RemainingMinutes = remainingMinutes
                };
            })
            .Where(o => o != null)
            .GroupBy(o => o!.Date)
            .OrderBy(g => g.Key);

        foreach (var dateGroup in ordersByDate)
        {
            var date = dateGroup.Key;
            var intervals = dateGroup
                .GroupBy(o => o!.IntervalStart)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var remainingTimes = g.Select(o => o!.RemainingMinutes).ToList();
                    var avg = remainingTimes.Average();
                    var count = remainingTimes.Count;

                    return new TimeIntervalData
                    {
                        TimeLabel = g.Key.ToString("HH:mm"),
                        IntervalStart = g.Key,
                        AverageRemainingMinutes = avg,
                        OrderCount = count
                    };
                })
                .ToList();

            // Fill in missing 15-minute intervals for the day (from 00:00 to 23:45)
            var allIntervals = new List<TimeIntervalData>();
            var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            for (int hour = 0; hour < 24; hour++)
            {
                for (int minute = 0; minute < 60; minute += 15)
                {
                    var intervalTime = dayStart.AddHours(hour).AddMinutes(minute);
                    var existing = intervals.FirstOrDefault(i => i.IntervalStart == intervalTime);
                    if (existing != null)
                    {
                        allIntervals.Add(existing);
                    }
                    else
                    {
                        allIntervals.Add(new TimeIntervalData
                        {
                            TimeLabel = intervalTime.ToString("HH:mm"),
                            IntervalStart = intervalTime,
                            AverageRemainingMinutes = null,
                            OrderCount = 0
                        });
                    }
                }
            }

            results.Add(new RemainingTimeAggregationDto
            {
                Date = date,
                DateLabel = date.ToString("dd.MM.yyyy"),
                Intervals = allIntervals
            });
        }

        return Ok(results);
    }

    [HttpGet("greifinn")]
    public async Task<IActionResult> GetGreifinnOrders(
        [FromQuery] string? phoneNumber,
        [FromQuery] string? customerName,
        [FromQuery] string? customerAddress,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? locationId,
        [FromQuery] int? deliveryMethodId,
        [FromQuery] int? paymentMethodId,
        [FromQuery] decimal? totalPrice,
        [FromQuery] string? externalId,
        [FromQuery] int? statusId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Adjust toDate: if fromDate and toDate are provided and represent a single day,
            // the system expects toDate to be the next day
            DateTime? adjustedToDate = toDate;
            if (fromDate.HasValue && toDate.HasValue)
            {
                // Check if this is a single day query (toDate is one day after fromDate)
                var dayAfter = fromDate.Value.Date.AddDays(1);
                if (toDate.Value.Date == dayAfter)
                {
                    // This is a single day query - keep as is
                    adjustedToDate = toDate;
                }
            }

            var result = await _greifinnOrderScraper.GetOrdersAsync(
                phoneNumber: phoneNumber,
                customerName: customerName,
                customerAddress: customerAddress,
                fromDate: fromDate,
                toDate: adjustedToDate,
                locationId: locationId,
                deliveryMethodId: deliveryMethodId,
                paymentMethodId: paymentMethodId,
                totalPrice: totalPrice,
                externalId: externalId,
                statusId: statusId,
                page: page,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching orders from Greifinn: {ex.Message}");
        }
    }

    [HttpGet("greifinn/{orderId}")]
    public async Task<IActionResult> GetGreifinnOrderDetail(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return BadRequest("Order ID is required");

        try
        {
            var result = await _greifinnOrderScraper.GetOrderDetailAsync(orderId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching order detail from Greifinn: {ex.Message}");
        }
    }
}


