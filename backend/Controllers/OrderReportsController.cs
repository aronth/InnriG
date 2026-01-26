using InnriGreifi.API.Data;
using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/orders/reports")]
[Authorize(Roles = "Admin")]
public class OrderReportsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IOrderReportingService _reportingService;
    private readonly OrderLateRules _orderLateRules;

    public OrderReportsController(AppDbContext context, IOrderReportingService reportingService, OrderLateRules orderLateRules)
    {
        _context = context;
        _reportingService = reportingService;
        _orderLateRules = orderLateRules;
    }

    /// <summary>
    /// High-level KPIs: total, evaluable, late ratio, avg/p90 wait time, orders by method/source.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<OrderReportSummaryDto>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? deliveryMethod,
        [FromQuery] Guid? restaurantId,
        CancellationToken ct)
    {
        var result = await _reportingService.GetSummaryAsync(from, to, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Complete dataset summary: all orders without date filtering.
    /// Returns KPIs for the entire dataset.
    /// </summary>
    [HttpGet("summary/complete")]
    public async Task<ActionResult<OrderReportSummaryDto>> GetCompleteSummary(CancellationToken ct)
    {
        var result = await _reportingService.GetCompleteSummaryAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Diagnostic endpoint to check OrderSource distribution in the database.
    /// </summary>
    [HttpGet("diagnostics/order-source")]
    public async Task<IActionResult> GetOrderSourceDiagnostics(CancellationToken ct)
    {
        var sources = await _context.OrderRows
            .AsNoTracking()
            .GroupBy(r => r.OrderSource ?? "NULL")
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var sample = await _context.OrderRows
            .AsNoTracking()
            .Where(r => r.OrderSource == "Counter")
            .Select(r => new { r.Id, r.OrderNumber, r.OrderSource })
            .Take(10)
            .ToListAsync(ct);

        return Ok(new
        {
            Distribution = sources,
            SampleCounterOrders = sample
        });
    }

    /// <summary>
    /// Wait time series: avg + p90 per day/week/month.
    /// </summary>
    [HttpGet("waittime-series")]
    public async Task<ActionResult<List<OrderWaitTimeSeriesPointDto>>> GetWaitTimeSeries(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string granularity = "day",
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetWaitTimeSeriesAsync(from, to, granularity, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Wait time distribution as histogram buckets.
    /// </summary>
    [HttpGet("waittime-distribution")]
    public async Task<ActionResult<OrderWaitTimeDistributionDto>> GetWaitTimeDistribution(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] int bucketSize = 5,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetWaitTimeDistributionAsync(from, to, deliveryMethod, bucketSize, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Late ratio series per day/week/month. If deliveryMethod is null, aggregates all methods.
    /// </summary>
    [HttpGet("late-series")]
    public async Task<ActionResult<List<OrderLateRatioPointDto>>> GetLateSeries(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string granularity = "day",
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetLateSeriesAsync(from, to, granularity, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Heatmap of order volume by weekday × hour.
    /// </summary>
    [HttpGet("heatmap/volume")]
    public async Task<ActionResult<OrderHeatmapDto>> GetVolumeHeatmap(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetVolumeHeatmapAsync(from, to, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Heatmap of late ratio by weekday × hour.
    /// </summary>
    [HttpGet("heatmap/late")]
    public async Task<ActionResult<OrderHeatmapDto>> GetLateHeatmap(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetLateHeatmapAsync(from, to, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Heatmap of average wait time by weekday × hour.
    /// </summary>
    [HttpGet("heatmap/waittime")]
    public async Task<ActionResult<OrderHeatmapDto>> GetWaitTimeHeatmap(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetWaitTimeHeatmapAsync(from, to, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Heatmap of P90 wait time by weekday × hour.
    /// </summary>
    [HttpGet("heatmap/p90-waittime")]
    public async Task<ActionResult<OrderHeatmapDto>> GetP90WaitTimeHeatmap(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetP90WaitTimeHeatmapAsync(from, to, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Volume comparison using weekday expected-index baseline.
    /// </summary>
    [HttpGet("volume-expected-index")]
    public async Task<ActionResult<OrderVolumeExpectedIndexDto>> GetVolumeExpectedIndex(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int baselineWindowDays = 56,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetVolumeExpectedIndexAsync(from, to, baselineWindowDays, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Week-by-week growth metrics with filters for delivery method, weekday, and late status.
    /// </summary>
    [HttpGet("growth-by-week")]
    public async Task<ActionResult<List<OrderGrowthWeekDto>>> GetGrowthByWeek(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] int? weekday = null,
        [FromQuery] bool? isLate = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetGrowthByWeekAsync(from, to, deliveryMethod, weekday, isLate, restaurantId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Period-over-period comparison for KPI dashboard (week or month).
    /// Compares current period vs previous period with all KPIs and change percentages.
    /// </summary>
    [HttpGet("period-comparison")]
    public async Task<ActionResult<PeriodComparisonDto>> GetPeriodComparison(
        [FromQuery] string periodType,
        [FromQuery] DateTime periodStart,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _reportingService.GetPeriodComparisonAsync(
                periodType,
                periodStart,
                deliveryMethod,
                restaurantId,
                ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Forecast/prediction for next period based on historical trends.
    /// Uses weekday seasonality and trend analysis to predict orders, revenue, and metrics.
    /// </summary>
    [HttpGet("forecast")]
    public async Task<ActionResult<ForecastDto>> GetForecast(
        [FromQuery] string periodType,
        [FromQuery] DateTime targetPeriodStart,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        [FromQuery] int lookbackDays = 56,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _reportingService.GetForecastAsync(
                periodType,
                targetPeriodStart,
                deliveryMethod,
                restaurantId,
                lookbackDays,
                ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Timeline report for a single day showing individual orders with their time metrics.
    /// Returns orders with wait time, time to scan, and time to checkout.
    /// </summary>
    [HttpGet("timeline")]
    public async Task<ActionResult<OrderTimelineReportDto>> GetTimeline(
        [FromQuery] DateTime date,
        [FromQuery] string? deliveryMethod = null,
        [FromQuery] Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var result = await _reportingService.GetTimelineAsync(date, deliveryMethod, restaurantId, ct);
        return Ok(result);
    }

    #region Legacy endpoints (kept for backward compatibility)

    /// <summary>
    /// Per user: late if "minutes left" is less than threshold.
    /// Sent: DeliveryDate - CheckedOutAt &lt; 15 minutes
    /// Other: DeliveryDate - ScannedAt &lt; 7 minutes
    /// </summary>
    [HttpGet("late-count-by-method")]
    public async Task<ActionResult<List<OrderLateCountByMethodDto>>> GetLateCountByMethod(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(-30);
        var toUtc = to?.ToUniversalTime() ?? DateTime.UtcNow;

        var rows = await _context.OrderRows
            .AsNoTracking()
            .Where(r => r.DeliveryDate != null)
            .Where(r => r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc)
            .Select(r => new
            {
                r.DeliveryMethod,
                r.DeliveryDate,
                r.CheckedOutAt,
                r.ScannedAt,
                r.ReadyTime
            })
            .ToListAsync(cancellationToken);

        var result = rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.DeliveryMethod) ? "Unknown" : r.DeliveryMethod)
            .Select(g =>
            {
                var total = g.Count();
                var evaluable = g.Count(x => _orderLateRules.IsEvaluable(g.Key, x.CheckedOutAt, x.ScannedAt));
                var late = g.Count(x => _orderLateRules.IsLate(g.Key, x.DeliveryDate!.Value, x.CheckedOutAt, x.ScannedAt, x.ReadyTime));

                return new OrderLateCountByMethodDto
                {
                    DeliveryMethod = g.Key,
                    Total = total,
                    Evaluable = evaluable,
                    LateCount = late
                };
            })
            .OrderBy(r => r.DeliveryMethod)
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Legacy: late ratio for a specific delivery method. Use late-series for all methods.
    /// </summary>
    [HttpGet("late-ratio")]
    public async Task<ActionResult<List<OrderLateRatioPointDto>>> GetLateRatio(
        [FromQuery] string deliveryMethod,
        [FromQuery] string granularity = "day",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deliveryMethod))
            return BadRequest("deliveryMethod is required");

        var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(-30);
        var toUtc = to?.ToUniversalTime() ?? DateTime.UtcNow;

        var method = deliveryMethod.Trim();
        var gran = granularity.Trim().ToLowerInvariant();
        if (gran is not ("day" or "week" or "month"))
            return BadRequest("granularity must be one of: day, week, month");

        var rows = await _context.OrderRows
            .AsNoTracking()
            .Where(r => r.DeliveryDate != null)
            .Where(r => r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc)
            .Where(r => r.DeliveryMethod == method)
            .Select(r => new
            {
                r.DeliveryDate,
                r.CheckedOutAt,
                r.ScannedAt,
                r.ReadyTime
            })
            .ToListAsync(cancellationToken);

        var points = rows
            .GroupBy(r => GetPeriodStart(r.DeliveryDate!.Value, gran))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var total = g.Count();
                var evaluable = g.Count(x => _orderLateRules.IsEvaluable(method, x.CheckedOutAt, x.ScannedAt));
                var late = g.Count(x => _orderLateRules.IsLate(method, x.DeliveryDate!.Value, x.CheckedOutAt, x.ScannedAt, x.ReadyTime));

                return new OrderLateRatioPointDto
                {
                    PeriodStart = g.Key,
                    Total = total,
                    Evaluable = evaluable,
                    LateCount = late
                };
            })
            .ToList();

        return Ok(points);
    }

    private static DateTime GetPeriodStart(DateTime utcDateTime, string granularity)
    {
        var d = utcDateTime.Date; // UTC
        return granularity switch
        {
            "day" => d,
            "week" => d.AddDays(-(GetIsoDayOfWeek(d) - 1)), // Monday as start
            "month" => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => d
        };
    }

    private static int GetIsoDayOfWeek(DateTime d)
    {
        // ISO: Monday=1 ... Sunday=7
        return d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;
    }

    #endregion
}
