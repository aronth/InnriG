using InnriGreifi.API.Data;
using InnriGreifi.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class OrderReportingService : IOrderReportingService
{
    private readonly AppDbContext _context;
    private readonly OrderLateRules _orderLateRules;

    private static readonly string[] WeekdayNames = 
    {
        "Mánudagur", "Þriðjudagur", "Miðvikudagur", "Fimmtudagur",
        "Föstudagur", "Laugardagur", "Sunnudagur"
    };

    public OrderReportingService(AppDbContext context, OrderLateRules orderLateRules)
    {
        _context = context;
        _orderLateRules = orderLateRules;
    }

    public async Task<OrderReportSummaryDto> GetSummaryAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.DeliveryMethod,
            r.OrderSource,
            r.DeliveryDate,
            r.CheckedOutAt,
            r.ScannedAt,
            r.ReadyTime,
            r.WaitTimeMin,
            r.TotalAmountWithVat
        }).ToListAsync(ct);

        var total = rows.Count;
        var evaluable = rows.Count(r => _orderLateRules.IsEvaluable(r.DeliveryMethod, r.CheckedOutAt, r.ScannedAt));
        var late = rows.Count(r => _orderLateRules.IsLate(r.DeliveryMethod, r.DeliveryDate!.Value, r.CheckedOutAt, r.ScannedAt, r.ReadyTime));

        var waitTimes = rows.Where(r => r.WaitTimeMin.HasValue).Select(r => r.WaitTimeMin!.Value).OrderBy(x => x).ToList();
        decimal? avgWait = waitTimes.Count > 0 ? (decimal)waitTimes.Average() : null;
        decimal? p90Wait = waitTimes.Count > 0 ? waitTimes[Math.Min((int)(waitTimes.Count * 0.9), waitTimes.Count - 1)] : null;

        var totalRevenue = rows.Where(r => r.TotalAmountWithVat.HasValue).Sum(r => r.TotalAmountWithVat!.Value);

        return new OrderReportSummaryDto
        {
            TotalOrders = total,
            EvaluableOrders = evaluable,
            LateOrders = late,
            AvgWaitTimeMin = avgWait,
            P90WaitTimeMin = p90Wait,
            TotalRevenue = totalRevenue,
            OrdersByMethod = rows.GroupBy(r => r.DeliveryMethod).ToDictionary(g => g.Key ?? "Unknown", g => g.Count()),
            RevenueByMethod = rows
                .Where(r => r.TotalAmountWithVat.HasValue)
                .GroupBy(r => r.DeliveryMethod)
                .ToDictionary(g => g.Key ?? "Unknown", g => g.Sum(r => r.TotalAmountWithVat!.Value)),
            OrdersBySource = rows
                .GroupBy(r => NormalizeOrderSource(r.OrderSource))
                .ToDictionary(g => g.Key, g => g.Count()),
            RevenueBySource = rows
                .Where(r => r.TotalAmountWithVat.HasValue)
                .GroupBy(r => NormalizeOrderSource(r.OrderSource))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalAmountWithVat!.Value))
        };
    }

    public async Task<OrderReportSummaryDto> GetCompleteSummaryAsync(
        CancellationToken ct = default)
    {
        // Get all orders without date filtering
        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null);

        var rows = await query.Select(r => new
        {
            r.DeliveryMethod,
            r.OrderSource,
            r.DeliveryDate,
            r.CheckedOutAt,
            r.ScannedAt,
            r.ReadyTime,
            r.WaitTimeMin,
            r.TotalAmountWithVat
        }).ToListAsync(ct);

        var total = rows.Count;
        var evaluable = rows.Count(r => _orderLateRules.IsEvaluable(r.DeliveryMethod, r.CheckedOutAt, r.ScannedAt));
        var late = rows.Count(r => _orderLateRules.IsLate(r.DeliveryMethod, r.DeliveryDate!.Value, r.CheckedOutAt, r.ScannedAt, r.ReadyTime));

        var waitTimes = rows.Where(r => r.WaitTimeMin.HasValue).Select(r => r.WaitTimeMin!.Value).OrderBy(x => x).ToList();
        decimal? avgWait = waitTimes.Count > 0 ? (decimal)waitTimes.Average() : null;
        decimal? p90Wait = waitTimes.Count > 0 ? waitTimes[Math.Min((int)(waitTimes.Count * 0.9), waitTimes.Count - 1)] : null;

        var totalRevenue = rows.Where(r => r.TotalAmountWithVat.HasValue).Sum(r => r.TotalAmountWithVat!.Value);

        return new OrderReportSummaryDto
        {
            TotalOrders = total,
            EvaluableOrders = evaluable,
            LateOrders = late,
            AvgWaitTimeMin = avgWait,
            P90WaitTimeMin = p90Wait,
            TotalRevenue = totalRevenue,
            OrdersByMethod = rows.GroupBy(r => r.DeliveryMethod ?? "Unknown").ToDictionary(g => g.Key, g => g.Count()),
            RevenueByMethod = rows
                .Where(r => r.TotalAmountWithVat.HasValue)
                .GroupBy(r => r.DeliveryMethod ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalAmountWithVat!.Value)),
            OrdersBySource = rows
                .GroupBy(r => NormalizeOrderSource(r.OrderSource))
                .ToDictionary(g => g.Key, g => g.Count()),
            RevenueBySource = rows
                .Where(r => r.TotalAmountWithVat.HasValue)
                .GroupBy(r => NormalizeOrderSource(r.OrderSource))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalAmountWithVat!.Value))
        };
    }

    public async Task<List<OrderWaitTimeSeriesPointDto>> GetWaitTimeSeriesAsync(
        DateTime? from,
        DateTime? to,
        string granularity = "day",
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);
        var gran = NormalizeGranularity(granularity);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc)
            .Where(r => r.WaitTimeMin != null);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.DeliveryDate,
            r.WaitTimeMin
        }).ToListAsync(ct);

        return rows
            .GroupBy(r => GetPeriodStart(r.DeliveryDate!.Value, gran))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var times = g.Select(x => x.WaitTimeMin!.Value).OrderBy(x => x).ToList();
                return new OrderWaitTimeSeriesPointDto
                {
                    PeriodStart = g.Key,
                    Count = times.Count,
                    AvgWaitTimeMin = times.Count > 0 ? (decimal)times.Average() : null,
                    P90WaitTimeMin = times.Count > 0 ? times[Math.Min((int)(times.Count * 0.9), times.Count - 1)] : null
                };
            })
            .ToList();
    }

    public async Task<OrderWaitTimeDistributionDto> GetWaitTimeDistributionAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        int bucketSize = 5,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);
        bucketSize = Math.Clamp(bucketSize, 1, 60);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc)
            .Where(r => r.WaitTimeMin != null);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var waitTimes = await query.Select(r => r.WaitTimeMin!.Value).ToListAsync(ct);

        // Define bucket ranges: 0-5, 5-10, ..., up to max observed time + overflow bucket
        var maxBucketStart = waitTimes.Count > 0 ? (waitTimes.Max() / bucketSize) * bucketSize : 60;
        maxBucketStart = Math.Max(maxBucketStart, 60); // At least up to 60 minutes

        var buckets = new List<OrderWaitTimeBucketDto>();
        for (int start = 0; start <= maxBucketStart; start += bucketSize)
        {
            var end = start + bucketSize;
            var isLast = start >= maxBucketStart;

            buckets.Add(new OrderWaitTimeBucketDto
            {
                MinMinutes = start,
                MaxMinutes = isLast ? null : end,
                Count = isLast
                    ? waitTimes.Count(t => t >= start)
                    : waitTimes.Count(t => t >= start && t < end)
            });
        }

        return new OrderWaitTimeDistributionDto
        {
            BucketSize = bucketSize,
            Buckets = buckets
        };
    }

    public async Task<List<OrderLateRatioPointDto>> GetLateSeriesAsync(
        DateTime? from,
        DateTime? to,
        string granularity = "day",
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);
        var gran = NormalizeGranularity(granularity);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.DeliveryMethod,
            r.DeliveryDate,
            r.CheckedOutAt,
            r.ScannedAt,
            r.ReadyTime
        }).ToListAsync(ct);

        return rows
            .GroupBy(r => GetPeriodStart(r.DeliveryDate!.Value, gran))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var total = g.Count();
                var evaluable = g.Count(x => _orderLateRules.IsEvaluable(x.DeliveryMethod, x.CheckedOutAt, x.ScannedAt));
                var late = g.Count(x => _orderLateRules.IsLate(x.DeliveryMethod, x.DeliveryDate!.Value, x.CheckedOutAt, x.ScannedAt, x.ReadyTime));

                return new OrderLateRatioPointDto
                {
                    PeriodStart = g.Key,
                    Total = total,
                    Evaluable = evaluable,
                    LateCount = late
                };
            })
            .ToList();
    }

    public async Task<OrderHeatmapDto> GetVolumeHeatmapAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => r.DeliveryDate!.Value).ToListAsync(ct);

        // Build cells matrix [weekday][hour]
        var cells = new List<List<OrderHeatmapCellDto>>();
        for (int wd = 0; wd < 7; wd++)
        {
            var hourCells = new List<OrderHeatmapCellDto>();
            for (int hr = 0; hr < 24; hr++)
            {
                var count = rows.Count(d => GetIsoWeekday(d) == wd && d.Hour == hr);
                hourCells.Add(new OrderHeatmapCellDto
                {
                    Weekday = wd,
                    Hour = hr,
                    Value = count
                });
            }
            cells.Add(hourCells);
        }

        return new OrderHeatmapDto
        {
            DataType = "volume",
            Cells = cells
        };
    }

    public async Task<OrderHeatmapDto> GetLateHeatmapAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.DeliveryMethod,
            r.DeliveryDate,
            r.CheckedOutAt,
            r.ScannedAt,
            r.ReadyTime
        }).ToListAsync(ct);

        // Build cells matrix [weekday][hour]
        var cells = new List<List<OrderHeatmapCellDto>>();
        for (int wd = 0; wd < 7; wd++)
        {
            var hourCells = new List<OrderHeatmapCellDto>();
            for (int hr = 0; hr < 24; hr++)
            {
                var cellRows = rows.Where(r => GetIsoWeekday(r.DeliveryDate!.Value) == wd && r.DeliveryDate!.Value.Hour == hr).ToList();
                var total = cellRows.Count;
                var evaluable = cellRows.Count(x => _orderLateRules.IsEvaluable(x.DeliveryMethod, x.CheckedOutAt, x.ScannedAt));
                var late = cellRows.Count(x => _orderLateRules.IsLate(x.DeliveryMethod, x.DeliveryDate!.Value, x.CheckedOutAt, x.ScannedAt, x.ReadyTime));

                hourCells.Add(new OrderHeatmapCellDto
                {
                    Weekday = wd,
                    Hour = hr,
                    Value = evaluable == 0 ? 0 : (decimal)late / evaluable,
                    Total = total,
                    Evaluable = evaluable,
                    LateCount = late
                });
            }
            cells.Add(hourCells);
        }

        return new OrderHeatmapDto
        {
            DataType = "lateRatio",
            Cells = cells
        };
    }

    public async Task<OrderHeatmapDto> GetWaitTimeHeatmapAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc)
            .Where(r => r.WaitTimeMin != null);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.DeliveryDate,
            r.WaitTimeMin
        }).ToListAsync(ct);

        // Build cells matrix [weekday][hour]
        var cells = new List<List<OrderHeatmapCellDto>>();
        for (int wd = 0; wd < 7; wd++)
        {
            var hourCells = new List<OrderHeatmapCellDto>();
            for (int hr = 0; hr < 24; hr++)
            {
                var cellRows = rows
                    .Where(r => GetIsoWeekday(r.DeliveryDate!.Value) == wd && r.DeliveryDate!.Value.Hour == hr)
                    .Select(r => r.WaitTimeMin!.Value)
                    .ToList();

                var count = cellRows.Count;
                var avgWaitTime = count > 0 ? (decimal)cellRows.Average() : 0;

                hourCells.Add(new OrderHeatmapCellDto
                {
                    Weekday = wd,
                    Hour = hr,
                    Value = avgWaitTime,
                    Total = count
                });
            }
            cells.Add(hourCells);
        }

        return new OrderHeatmapDto
        {
            DataType = "waitTime",
            Cells = cells
        };
    }

    public async Task<OrderHeatmapDto> GetP90WaitTimeHeatmapAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = NormalizeDateRange(from, to);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc)
            .Where(r => r.WaitTimeMin != null);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.DeliveryDate,
            r.WaitTimeMin
        }).ToListAsync(ct);

        // Build cells matrix [weekday][hour]
        var cells = new List<List<OrderHeatmapCellDto>>();
        for (int wd = 0; wd < 7; wd++)
        {
            var hourCells = new List<OrderHeatmapCellDto>();
            for (int hr = 0; hr < 24; hr++)
            {
                var cellRows = rows
                    .Where(r => GetIsoWeekday(r.DeliveryDate!.Value) == wd && r.DeliveryDate!.Value.Hour == hr)
                    .Select(r => r.WaitTimeMin!.Value)
                    .OrderBy(x => x)
                    .ToList();

                var count = cellRows.Count;
                var p90WaitTime = count > 0 
                    ? cellRows[Math.Min((int)(count * 0.9), count - 1)] 
                    : 0;

                hourCells.Add(new OrderHeatmapCellDto
                {
                    Weekday = wd,
                    Hour = hr,
                    Value = p90WaitTime,
                    Total = count
                });
            }
            cells.Add(hourCells);
        }

        return new OrderHeatmapDto
        {
            DataType = "p90WaitTime",
            Cells = cells
        };
    }

    public async Task<OrderVolumeExpectedIndexDto> GetVolumeExpectedIndexAsync(
        DateTime from,
        DateTime to,
        int baselineWindowDays = 56,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var fromUtc = from.ToUniversalTime().Date;
        var toUtc = to.ToUniversalTime().Date.AddDays(1).AddTicks(-1); // end of day

        // Baseline: N days before the target period starts
        var baselineEnd = fromUtc.AddDays(-1);
        var baselineStart = baselineEnd.AddDays(-baselineWindowDays + 1);

        // Fetch baseline data
        var baselineQuery = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= baselineStart && r.DeliveryDate <= baselineEnd);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            baselineQuery = baselineQuery.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            baselineQuery = baselineQuery.Where(r => r.RestaurantId == restaurantId.Value);

        var baselineRows = await baselineQuery.Select(r => r.DeliveryDate!.Value).ToListAsync(ct);

        // If baseline has insufficient data, use all available data before target period
        // Check if we have at least 7 unique days of data in baseline (one full week)
        var baselineUniqueDays = baselineRows.Select(d => d.Date).Distinct().Count();
        if (baselineUniqueDays < 7)
        {
            // Find the earliest available data before the target period
            var earliestQuery = _context.OrderRows.AsNoTracking()
                .Where(r => r.DeliveryDate != null && r.DeliveryDate < fromUtc);
            
            if (!string.IsNullOrWhiteSpace(deliveryMethod))
                earliestQuery = earliestQuery.Where(r => r.DeliveryMethod == deliveryMethod);
            
            if (restaurantId != null)
                earliestQuery = earliestQuery.Where(r => r.RestaurantId == restaurantId.Value);
            
            var earliestDate = await earliestQuery
                .OrderBy(r => r.DeliveryDate)
                .Select(r => r.DeliveryDate!.Value.Date)
                .FirstOrDefaultAsync(ct);
            
            if (earliestDate != default && earliestDate < fromUtc)
            {
                // Use all available data from earliest date to day before target period
                baselineStart = earliestDate;
                baselineEnd = fromUtc.AddDays(-1);
                
                baselineQuery = _context.OrderRows.AsNoTracking()
                    .Where(r => r.DeliveryDate != null && r.DeliveryDate >= baselineStart && r.DeliveryDate <= baselineEnd);
                
                if (!string.IsNullOrWhiteSpace(deliveryMethod))
                    baselineQuery = baselineQuery.Where(r => r.DeliveryMethod == deliveryMethod);
                
                if (restaurantId != null)
                    baselineQuery = baselineQuery.Where(r => r.RestaurantId == restaurantId.Value);
                
                baselineRows = await baselineQuery.Select(r => r.DeliveryDate!.Value).ToListAsync(ct);
            }
        }

        // Count orders per weekday in baseline
        var baselineByWeekday = new int[7];
        var daysPerWeekday = new int[7];
        for (var d = baselineStart; d <= baselineEnd; d = d.AddDays(1))
        {
            var wd = GetIsoWeekday(d);
            daysPerWeekday[wd]++;
        }
        foreach (var deliveryDate in baselineRows)
        {
            var wd = GetIsoWeekday(deliveryDate);
            baselineByWeekday[wd]++;
        }

        // Compute expected avg per weekday
        var avgPerWeekday = new decimal[7];
        for (int wd = 0; wd < 7; wd++)
        {
            avgPerWeekday[wd] = daysPerWeekday[wd] > 0 ? (decimal)baselineByWeekday[wd] / daysPerWeekday[wd] : 0;
        }

        // Fetch target period data
        var targetQuery = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            targetQuery = targetQuery.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            targetQuery = targetQuery.Where(r => r.RestaurantId == restaurantId.Value);

        var targetRows = await targetQuery.Select(r => r.DeliveryDate!.Value).ToListAsync(ct);

        // Count actual and days per weekday in target period
        var targetByWeekday = new int[7];
        var targetDaysPerWeekday = new int[7];
        for (var d = fromUtc; d <= toUtc.Date; d = d.AddDays(1))
        {
            var wd = GetIsoWeekday(d);
            targetDaysPerWeekday[wd]++;
        }
        foreach (var deliveryDate in targetRows)
        {
            var wd = GetIsoWeekday(deliveryDate);
            targetByWeekday[wd]++;
        }

        // Compute expected and actual totals
        var actualTotal = targetRows.Count;
        decimal expectedTotal = 0;
        for (int wd = 0; wd < 7; wd++)
        {
            expectedTotal += avgPerWeekday[wd] * targetDaysPerWeekday[wd];
        }

        var byWeekday = new List<WeekdayExpectedIndexDto>();
        for (int wd = 0; wd < 7; wd++)
        {
            byWeekday.Add(new WeekdayExpectedIndexDto
            {
                Weekday = wd,
                WeekdayName = WeekdayNames[wd],
                DaysInPeriod = targetDaysPerWeekday[wd],
                ActualCount = targetByWeekday[wd],
                BaselineAvgPerDay = avgPerWeekday[wd]
            });
        }

        return new OrderVolumeExpectedIndexDto
        {
            PeriodStart = fromUtc,
            PeriodEnd = toUtc.Date,
            BaselineWindowDays = baselineWindowDays,
            ActualTotal = actualTotal,
            ExpectedTotal = expectedTotal,
            ByWeekday = byWeekday
        };
    }

    public async Task<List<OrderGrowthWeekDto>> GetGrowthByWeekAsync(
        DateTime from,
        DateTime to,
        string? deliveryMethod = null,
        int? weekday = null,
        bool? isLate = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var fromUtc = from.ToUniversalTime().Date;
        var toUtc = to.ToUniversalTime().Date.AddDays(1).AddTicks(-1);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.DeliveryMethod,
            r.DeliveryDate,
            r.CheckedOutAt,
            r.ScannedAt,
            r.ReadyTime,
            r.WaitTimeMin
        }).ToListAsync(ct);

        // Apply weekday filter in memory (since GetIsoWeekday can't be translated to SQL)
        if (weekday.HasValue)
        {
            rows = rows.Where(r =>
            {
                if (r.DeliveryDate == null) return false;
                return GetIsoWeekday(r.DeliveryDate.Value) == weekday.Value;
            }).ToList();
        }

        // Apply late filter in memory (since it requires evaluation)
        if (isLate.HasValue)
        {
            rows = rows.Where(r =>
            {
                if (r.DeliveryDate == null) return false;
                var late = _orderLateRules.IsLate(r.DeliveryMethod, r.DeliveryDate.Value, r.CheckedOutAt, r.ScannedAt, r.ReadyTime);
                return late == isLate.Value;
            }).ToList();
        }

        // Group by week (Monday to Sunday)
        var weeks = rows
            .GroupBy(r => GetWeekStart(r.DeliveryDate!.Value))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var weekStart = g.Key;
                var weekEnd = weekStart.AddDays(6);

                var weekRows = g.ToList();
                var total = weekRows.Count;
                var evaluable = weekRows.Count(x => _orderLateRules.IsEvaluable(x.DeliveryMethod, x.CheckedOutAt, x.ScannedAt));
                var late = weekRows.Count(x => _orderLateRules.IsLate(x.DeliveryMethod, x.DeliveryDate!.Value, x.CheckedOutAt, x.ScannedAt, x.ReadyTime));

                var waitTimes = weekRows.Where(r => r.WaitTimeMin.HasValue).Select(r => r.WaitTimeMin!.Value).OrderBy(x => x).ToList();
                var avgWaitTime = waitTimes.Count > 0 ? (decimal?)waitTimes.Average() : null;
                var p90WaitTime = waitTimes.Count > 0 ? (decimal?)waitTimes[Math.Min((int)(waitTimes.Count * 0.9), waitTimes.Count - 1)] : null;

                return new OrderGrowthWeekDto
                {
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    TotalOrders = total,
                    EvaluableOrders = evaluable,
                    LateOrders = late,
                    AvgWaitTimeMin = avgWaitTime,
                    P90WaitTimeMin = p90WaitTime
                };
            })
            .ToList();

        // Calculate growth percentages
        for (int i = 0; i < weeks.Count; i++)
        {
            if (i == 0)
            {
                // First week has no previous week to compare
                continue;
            }

            var current = weeks[i];
            var previous = weeks[i - 1];

            // Calculate growth percentages
            current.TotalOrdersGrowth = previous.TotalOrders == 0
                ? null
                : ((decimal)current.TotalOrders - previous.TotalOrders) / previous.TotalOrders * 100;

            current.EvaluableOrdersGrowth = previous.EvaluableOrders == 0
                ? null
                : ((decimal)current.EvaluableOrders - previous.EvaluableOrders) / previous.EvaluableOrders * 100;

            current.LateOrdersGrowth = previous.LateOrders == 0
                ? null
                : ((decimal)current.LateOrders - previous.LateOrders) / previous.LateOrders * 100;

            current.LateRatioGrowth = previous.LateRatio == 0
                ? null
                : (current.LateRatio - previous.LateRatio) / previous.LateRatio * 100;

            current.AvgWaitTimeGrowth = previous.AvgWaitTimeMin.HasValue && current.AvgWaitTimeMin.HasValue
                ? (current.AvgWaitTimeMin.Value - previous.AvgWaitTimeMin.Value) / previous.AvgWaitTimeMin.Value * 100
                : null;

            current.P90WaitTimeGrowth = previous.P90WaitTimeMin.HasValue && current.P90WaitTimeMin.HasValue
                ? (current.P90WaitTimeMin.Value - previous.P90WaitTimeMin.Value) / previous.P90WaitTimeMin.Value * 100
                : null;
        }

        return weeks;
    }

    public async Task<PeriodComparisonDto> GetPeriodComparisonAsync(
        string periodType,
        DateTime periodStart,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        periodType = periodType?.Trim().ToLowerInvariant() ?? "week";
        if (periodType is not ("week" or "month"))
            throw new ArgumentException("periodType must be 'week' or 'month'", nameof(periodType));

        var periodStartUtc = periodStart.ToUniversalTime().Date;
        DateTime currentPeriodEnd;
        DateTime previousPeriodStart;
        DateTime previousPeriodEnd;

        if (periodType == "week")
        {
            // Week: Monday to Sunday
            var weekday = GetIsoWeekday(periodStartUtc);
            var weekStart = periodStartUtc.AddDays(-weekday);
            currentPeriodEnd = weekStart.AddDays(6).Date.AddDays(1).AddTicks(-1); // End of Sunday

            // Previous week
            previousPeriodStart = weekStart.AddDays(-7);
            previousPeriodEnd = previousPeriodStart.AddDays(6).Date.AddDays(1).AddTicks(-1);
        }
        else // month
        {
            // Month: first day to last day
            currentPeriodEnd = new DateTime(periodStartUtc.Year, periodStartUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(1).AddTicks(-1); // End of last day of month

            // Previous month
            var prevMonth = periodStartUtc.AddMonths(-1);
            previousPeriodStart = new DateTime(prevMonth.Year, prevMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            previousPeriodEnd = previousPeriodStart.AddMonths(1).AddTicks(-1);
        }

        var currentPeriod = await GetPeriodInfoAsync(
            periodStartUtc,
            currentPeriodEnd,
            deliveryMethod,
            restaurantId,
            ct);

        var previousPeriod = await GetPeriodInfoAsync(
            previousPeriodStart,
            previousPeriodEnd,
            deliveryMethod,
            restaurantId,
            ct);

        var comparison = CalculateComparison(currentPeriod, previousPeriod);

        return new PeriodComparisonDto
        {
            CurrentPeriod = currentPeriod,
            PreviousPeriod = previousPeriod,
            Comparison = comparison
        };
    }

    public async Task<ForecastDto> GetForecastAsync(
        string periodType,
        DateTime targetPeriodStart,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        int lookbackDays = 56,
        CancellationToken ct = default)
    {
        periodType = periodType?.Trim().ToLowerInvariant() ?? "week";
        if (periodType is not ("week" or "month"))
            throw new ArgumentException("periodType must be 'week' or 'month'", nameof(periodType));

        var targetStartUtc = targetPeriodStart.ToUniversalTime().Date;
        DateTime targetEndUtc;

        if (periodType == "week")
        {
            var weekday = GetIsoWeekday(targetStartUtc);
            var weekStart = targetStartUtc.AddDays(-weekday);
            targetEndUtc = weekStart.AddDays(6).Date.AddDays(1).AddTicks(-1);
        }
        else // month
        {
            targetEndUtc = new DateTime(targetStartUtc.Year, targetStartUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(1).AddTicks(-1);
        }

        // Baseline: lookbackDays before target period
        var baselineEnd = targetStartUtc.AddDays(-1);
        var baselineStart = baselineEnd.AddDays(-lookbackDays + 1);

        // Get baseline data
        var baselineQuery = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= baselineStart && r.DeliveryDate <= baselineEnd);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            baselineQuery = baselineQuery.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            baselineQuery = baselineQuery.Where(r => r.RestaurantId == restaurantId.Value);

        var baselineRows = await baselineQuery.Select(r => new
        {
            r.DeliveryDate,
            r.TotalAmountWithVat,
            r.DeliveryMethod,
            r.CheckedOutAt,
            r.ScannedAt,
            r.ReadyTime,
            r.WaitTimeMin
        }).ToListAsync(ct);

        // Calculate weekday averages from baseline
        var baselineByWeekday = new Dictionary<int, List<decimal>>(); // weekday -> list of daily totals
        var baselineRevenueByWeekday = new Dictionary<int, List<decimal>>();
        var baselineWaitTimesByWeekday = new Dictionary<int, List<int>>();
        var baselineLateRatiosByWeekday = new Dictionary<int, List<decimal>>();

        for (int wd = 0; wd < 7; wd++)
        {
            baselineByWeekday[wd] = new List<decimal>();
            baselineRevenueByWeekday[wd] = new List<decimal>();
            baselineWaitTimesByWeekday[wd] = new List<int>();
            baselineLateRatiosByWeekday[wd] = new List<decimal>();
        }

        // Group baseline by day, then by weekday
        var baselineByDay = baselineRows
            .GroupBy(r => r.DeliveryDate!.Value.Date)
            .ToList();

        foreach (var dayGroup in baselineByDay)
        {
            var day = dayGroup.Key;
            var wd = GetIsoWeekday(day);
            var dayOrders = dayGroup.ToList();
            var dayCount = dayOrders.Count;
            var dayRevenue = dayOrders.Where(r => r.TotalAmountWithVat.HasValue).Sum(r => r.TotalAmountWithVat!.Value);
            var dayWaitTimes = dayOrders.Where(r => r.WaitTimeMin.HasValue).Select(r => r.WaitTimeMin!.Value).ToList();
            var dayEvaluable = dayOrders.Count(r => _orderLateRules.IsEvaluable(r.DeliveryMethod, r.CheckedOutAt, r.ScannedAt));
            var dayLate = dayOrders.Count(r => _orderLateRules.IsLate(r.DeliveryMethod, r.DeliveryDate!.Value, r.CheckedOutAt, r.ScannedAt, r.ReadyTime));
            var dayLateRatio = dayEvaluable == 0 ? 0 : (decimal)dayLate / dayEvaluable;

            baselineByWeekday[wd].Add(dayCount);
            baselineRevenueByWeekday[wd].Add(dayRevenue);
            if (dayWaitTimes.Any())
                baselineWaitTimesByWeekday[wd].AddRange(dayWaitTimes);
            baselineLateRatiosByWeekday[wd].Add(dayLateRatio);
        }

        // Calculate averages per weekday
        var avgOrdersByWeekday = new Dictionary<int, decimal>();
        var avgRevenueByWeekday = new Dictionary<int, decimal>();
        var avgWaitTimeByWeekday = new Dictionary<int, decimal?>();
        var avgLateRatioByWeekday = new Dictionary<int, decimal>();

        for (int wd = 0; wd < 7; wd++)
        {
            avgOrdersByWeekday[wd] = baselineByWeekday[wd].Count > 0
                ? baselineByWeekday[wd].Average()
                : 0;
            avgRevenueByWeekday[wd] = baselineRevenueByWeekday[wd].Count > 0
                ? baselineRevenueByWeekday[wd].Average()
                : 0;
            avgWaitTimeByWeekday[wd] = baselineWaitTimesByWeekday[wd].Count > 0
                ? (decimal?)baselineWaitTimesByWeekday[wd].Average()
                : null;
            avgLateRatioByWeekday[wd] = baselineLateRatiosByWeekday[wd].Count > 0
                ? baselineLateRatiosByWeekday[wd].Average()
                : 0;
        }

        // Calculate standard deviations for confidence intervals
        var stdDevOrdersByWeekday = new Dictionary<int, decimal>();
        var stdDevRevenueByWeekday = new Dictionary<int, decimal>();

        for (int wd = 0; wd < 7; wd++)
        {
            var orders = baselineByWeekday[wd];
            if (orders.Count > 1)
            {
                var avg = avgOrdersByWeekday[wd];
                var variance = orders.Sum(x => (x - avg) * (x - avg)) / orders.Count;
                stdDevOrdersByWeekday[wd] = (decimal)Math.Sqrt((double)variance);
            }
            else
            {
                stdDevOrdersByWeekday[wd] = 0;
            }

            var revenues = baselineRevenueByWeekday[wd];
            if (revenues.Count > 1)
            {
                var avg = avgRevenueByWeekday[wd];
                var variance = revenues.Sum(x => (x - avg) * (x - avg)) / revenues.Count;
                stdDevRevenueByWeekday[wd] = (decimal)Math.Sqrt((double)variance);
            }
            else
            {
                stdDevRevenueByWeekday[wd] = 0;
            }
        }

        // Count days per weekday in target period
        var daysPerWeekday = new int[7];
        for (var d = targetStartUtc; d <= targetEndUtc.Date; d = d.AddDays(1))
        {
            var wd = GetIsoWeekday(d);
            daysPerWeekday[wd]++;
        }

        // Calculate predictions
        decimal predictedOrders = 0;
        decimal predictedRevenue = 0;
        decimal predictedLateRatio = 0;
        decimal? predictedAvgWaitTime = null;
        decimal predictedOrdersMin = 0;
        decimal predictedOrdersMax = 0;
        decimal predictedRevenueMin = 0;
        decimal predictedRevenueMax = 0;

        var waitTimeSum = 0m;
        var waitTimeCount = 0;

        var byWeekday = new List<ForecastWeekdayDto>();

        for (int wd = 0; wd < 7; wd++)
        {
            var days = daysPerWeekday[wd];
            var weekdayOrders = avgOrdersByWeekday[wd] * days;
            var weekdayRevenue = avgRevenueByWeekday[wd] * days;
            var weekdayStdDevOrders = stdDevOrdersByWeekday[wd] * (decimal)Math.Sqrt(days);
            var weekdayStdDevRevenue = stdDevRevenueByWeekday[wd] * (decimal)Math.Sqrt(days);

            predictedOrders += weekdayOrders;
            predictedRevenue += weekdayRevenue;
            predictedOrdersMin += weekdayOrders - weekdayStdDevOrders;
            predictedOrdersMax += weekdayOrders + weekdayStdDevOrders;
            predictedRevenueMin += weekdayRevenue - weekdayStdDevRevenue;
            predictedRevenueMax += weekdayRevenue + weekdayStdDevRevenue;

            if (avgWaitTimeByWeekday[wd].HasValue)
            {
                waitTimeSum += avgWaitTimeByWeekday[wd]!.Value * days;
                waitTimeCount += days;
            }

            predictedLateRatio += avgLateRatioByWeekday[wd] * days;

            byWeekday.Add(new ForecastWeekdayDto
            {
                Weekday = wd,
                WeekdayName = WeekdayNames[wd],
                DaysInPeriod = days,
                PredictedOrders = (int)Math.Round(weekdayOrders),
                PredictedRevenue = weekdayRevenue
            });
        }

        predictedLateRatio /= daysPerWeekday.Sum();
        predictedAvgWaitTime = waitTimeCount > 0 ? waitTimeSum / waitTimeCount : null;

        // Calculate trend direction and strength
        // Use recent weeks/months to determine trend
        var recentPeriods = new List<decimal>();
        var periodLength = periodType == "week" ? 7 : 30;
        var numPeriods = Math.Min(4, lookbackDays / periodLength);

        for (int i = 0; i < numPeriods; i++)
        {
            var periodStart = baselineEnd.AddDays(-(i + 1) * periodLength);
            var periodEnd = baselineEnd.AddDays(-i * periodLength);
            var periodRows = baselineRows
                .Where(r => r.DeliveryDate >= periodStart && r.DeliveryDate <= periodEnd)
                .ToList();
            recentPeriods.Add(periodRows.Count);
        }

        string trendDirection = "stable";
        decimal trendStrength = 0;

        if (recentPeriods.Count >= 2)
        {
            // Simple linear trend
            var recent = recentPeriods.Take(2).ToList();
            var older = recentPeriods.Skip(2).Take(2).ToList();

            if (recent.Count == 2 && older.Count >= 1)
            {
                var recentAvg = recent.Average();
                var olderAvg = older.Average();
                var change = (recentAvg - olderAvg) / (olderAvg == 0 ? 1 : olderAvg) * 100;

                if (change > 5)
                {
                    trendDirection = "improving";
                    trendStrength = Math.Min(100, Math.Abs(change) * 2);
                }
                else if (change < -5)
                {
                    trendDirection = "declining";
                    trendStrength = Math.Min(100, Math.Abs(change) * 2);
                }
                else
                {
                    trendDirection = "stable";
                    trendStrength = Math.Max(0, 50 - Math.Abs(change) * 5);
                }
            }
        }

        return new ForecastDto
        {
            ForecastPeriodStart = targetStartUtc,
            ForecastPeriodEnd = targetEndUtc,
            PredictedOrders = (int)Math.Round(predictedOrders),
            PredictedOrdersMin = Math.Max(0, (int)Math.Round(predictedOrdersMin)),
            PredictedOrdersMax = (int)Math.Round(predictedOrdersMax),
            PredictedRevenue = predictedRevenue,
            PredictedRevenueMin = Math.Max(0, predictedRevenueMin),
            PredictedRevenueMax = predictedRevenueMax,
            PredictedLateRatio = predictedLateRatio,
            PredictedAvgWaitTime = predictedAvgWaitTime,
            TrendDirection = trendDirection,
            TrendStrength = trendStrength,
            BaselineWindowDays = lookbackDays,
            ByWeekday = byWeekday
        };
    }

    private async Task<PeriodInfo> GetPeriodInfoAsync(
        DateTime fromUtc,
        DateTime toUtc,
        string? deliveryMethod,
        Guid? restaurantId,
        CancellationToken ct)
    {
        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.DeliveryDate != null && r.DeliveryDate >= fromUtc && r.DeliveryDate <= toUtc);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.DeliveryDate,
            r.DeliveryMethod,
            r.OrderSource,
            r.TotalAmountWithVat,
            r.CheckedOutAt,
            r.ScannedAt,
            r.ReadyTime,
            r.WaitTimeMin
        }).ToListAsync(ct);

        var totalOrders = rows.Count;
        var totalRevenue = rows.Where(r => r.TotalAmountWithVat.HasValue).Sum(r => r.TotalAmountWithVat!.Value);
        var evaluable = rows.Count(r => _orderLateRules.IsEvaluable(r.DeliveryMethod, r.CheckedOutAt, r.ScannedAt));
        var late = rows.Count(r => _orderLateRules.IsLate(r.DeliveryMethod, r.DeliveryDate!.Value, r.CheckedOutAt, r.ScannedAt, r.ReadyTime));

        var waitTimes = rows.Where(r => r.WaitTimeMin.HasValue).Select(r => r.WaitTimeMin!.Value).OrderBy(x => x).ToList();
        decimal? avgWait = waitTimes.Count > 0 ? (decimal)waitTimes.Average() : null;
        decimal? p90Wait = waitTimes.Count > 0 ? waitTimes[Math.Min((int)(waitTimes.Count * 0.9), waitTimes.Count - 1)] : null;

        // By day of week
        var byDayOfWeek = new List<DayOfWeekMetrics>();
        for (int wd = 0; wd < 7; wd++)
        {
            var dayRows = rows.Where(r => GetIsoWeekday(r.DeliveryDate!.Value) == wd).ToList();
            var dayOrders = dayRows.Count;
            var dayRevenue = dayRows.Where(r => r.TotalAmountWithVat.HasValue).Sum(r => r.TotalAmountWithVat!.Value);
            var dayEvaluable = dayRows.Count(r => _orderLateRules.IsEvaluable(r.DeliveryMethod, r.CheckedOutAt, r.ScannedAt));
            var dayLate = dayRows.Count(r => _orderLateRules.IsLate(r.DeliveryMethod, r.DeliveryDate!.Value, r.CheckedOutAt, r.ScannedAt));
            var dayLateRatio = dayEvaluable == 0 ? 0 : (decimal)dayLate / dayEvaluable;
            var dayWaitTimes = dayRows.Where(r => r.WaitTimeMin.HasValue).Select(r => r.WaitTimeMin!.Value).ToList();
            decimal? dayAvgWait = dayWaitTimes.Count > 0 ? (decimal)dayWaitTimes.Average() : null;

            byDayOfWeek.Add(new DayOfWeekMetrics
            {
                Weekday = wd,
                WeekdayName = WeekdayNames[wd],
                OrderCount = dayOrders,
                Revenue = dayRevenue,
                LateOrders = dayLate,
                LateRatio = dayLateRatio,
                AvgWaitTimeMin = dayAvgWait
            });
        }

        return new PeriodInfo
        {
            Start = fromUtc,
            End = toUtc,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            EvaluableOrders = evaluable,
            LateOrders = late,
            AvgWaitTimeMin = avgWait,
            P90WaitTimeMin = p90Wait,
            OrdersByMethod = rows.GroupBy(r => r.DeliveryMethod).ToDictionary(g => g.Key, g => g.Count()),
            RevenueByMethod = rows
                .Where(r => r.TotalAmountWithVat.HasValue)
                .GroupBy(r => r.DeliveryMethod)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalAmountWithVat!.Value)),
            OrdersBySource = rows
                .GroupBy(r => NormalizeOrderSource(r.OrderSource))
                .ToDictionary(g => g.Key, g => g.Count()),
            RevenueBySource = rows
                .Where(r => r.TotalAmountWithVat.HasValue)
                .GroupBy(r => NormalizeOrderSource(r.OrderSource))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalAmountWithVat!.Value)),
            ByDayOfWeek = byDayOfWeek
        };
    }

    private static ComparisonMetrics CalculateComparison(PeriodInfo current, PeriodInfo previous)
    {
        var comparison = new ComparisonMetrics();

        // Orders change
        comparison.OrdersChangePercent = previous.TotalOrders == 0
            ? 0
            : ((decimal)current.TotalOrders - previous.TotalOrders) / previous.TotalOrders * 100;

        // Revenue change
        comparison.RevenueChangePercent = previous.TotalRevenue == 0
            ? 0
            : (current.TotalRevenue - previous.TotalRevenue) / previous.TotalRevenue * 100;

        // Late ratio change
        comparison.LateRatioChangePercent = previous.LateRatio == 0
            ? 0
            : (current.LateRatio - previous.LateRatio) / previous.LateRatio * 100;

        // Wait time change
        if (previous.AvgWaitTimeMin.HasValue && current.AvgWaitTimeMin.HasValue)
        {
            comparison.WaitTimeChangePercent = (current.AvgWaitTimeMin.Value - previous.AvgWaitTimeMin.Value) 
                / previous.AvgWaitTimeMin.Value * 100;
        }

        if (previous.P90WaitTimeMin.HasValue && current.P90WaitTimeMin.HasValue)
        {
            comparison.P90WaitTimeChangePercent = (current.P90WaitTimeMin.Value - previous.P90WaitTimeMin.Value) 
                / previous.P90WaitTimeMin.Value * 100;
        }

        // By method changes
        foreach (var method in current.OrdersByMethod.Keys.Union(previous.OrdersByMethod.Keys))
        {
            var currentCount = current.OrdersByMethod.GetValueOrDefault(method, 0);
            var previousCount = previous.OrdersByMethod.GetValueOrDefault(method, 0);
            comparison.OrdersByMethodChangePercent[method] = previousCount == 0
                ? 0
                : ((decimal)currentCount - previousCount) / previousCount * 100;
        }

        foreach (var method in current.RevenueByMethod.Keys.Union(previous.RevenueByMethod.Keys))
        {
            var currentRev = current.RevenueByMethod.GetValueOrDefault(method, 0);
            var previousRev = previous.RevenueByMethod.GetValueOrDefault(method, 0);
            comparison.RevenueByMethodChangePercent[method] = previousRev == 0
                ? 0
                : (currentRev - previousRev) / previousRev * 100;
        }

        return comparison;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        // Get Monday of the week (ISO weekday 0 = Monday)
        var weekday = GetIsoWeekday(date);
        return date.Date.AddDays(-weekday);
    }

    #region Helpers

    private static (DateTime fromUtc, DateTime toUtc) NormalizeDateRange(DateTime? from, DateTime? to)
    {
        var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(-30);
        var toUtc = to?.ToUniversalTime() ?? DateTime.UtcNow;
        return (fromUtc, toUtc);
    }

    private static string NormalizeGranularity(string granularity)
    {
        var g = granularity?.Trim().ToLowerInvariant() ?? "day";
        return g is "day" or "week" or "month" ? g : "day";
    }

    private static DateTime GetPeriodStart(DateTime utcDateTime, string granularity)
    {
        var d = utcDateTime.Date;
        return granularity switch
        {
            "day" => d,
            "week" => d.AddDays(-(GetIsoWeekday(d))), // Monday as start (ISO weekday 0 = Monday)
            "month" => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => d
        };
    }

    /// <summary>
    /// Returns ISO weekday: 0=Monday..6=Sunday.
    /// </summary>
    private static int GetIsoWeekday(DateTime d)
    {
        // .NET: Sunday=0, Monday=1, ..., Saturday=6
        // ISO: Monday=0, Tuesday=1, ..., Sunday=6
        return d.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)d.DayOfWeek - 1;
    }

    private static string NormalizeOrderSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "Unknown";
        
        var trimmed = source.Trim();
        // Normalize case for consistency
        return trimmed.Equals("counter", StringComparison.OrdinalIgnoreCase) ? "Counter" :
               trimmed.Equals("web", StringComparison.OrdinalIgnoreCase) ? "Web" :
               trimmed;
    }

    public async Task<OrderTimelineReportDto> GetTimelineAsync(
        DateTime date,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default)
    {
        var dateUtc = date.ToUniversalTime().Date;
        var nextDayUtc = dateUtc.AddDays(1);

        var query = _context.OrderRows.AsNoTracking()
            .Where(r => r.CreatedDate != null && r.CreatedDate >= dateUtc && r.CreatedDate < nextDayUtc);

        if (!string.IsNullOrWhiteSpace(deliveryMethod))
            query = query.Where(r => r.DeliveryMethod == deliveryMethod);

        if (restaurantId != null)
            query = query.Where(r => r.RestaurantId == restaurantId.Value);

        var rows = await query.Select(r => new
        {
            r.Id,
            r.CreatedDate,
            r.DeliveryDate,
            r.ScannedAt,
            r.CheckedOutAt,
            r.WaitTimeMin,
            r.DeliveryMethod,
            r.RestaurantId,
            r.OrderNumber,
            r.TotalAmountWithVat
        }).OrderBy(r => r.CreatedDate).ToListAsync(ct);

        var orders = new List<OrderTimelinePointDto>();

        foreach (var row in rows)
        {
            if (row.CreatedDate == null) continue;

            var orderTime = row.CreatedDate.Value;
            
            // Calculate time to scan (minutes from order creation to scan)
            decimal? timeToScan = null;
            if (row.ScannedAt != null)
            {
                var scanDelta = row.ScannedAt.Value - orderTime;
                timeToScan = (decimal)scanDelta.TotalMinutes;
            }

            // Calculate time to checkout (minutes from order creation to checkout)
            decimal? timeToCheckout = null;
            if (row.CheckedOutAt != null)
            {
                var checkoutDelta = row.CheckedOutAt.Value - orderTime;
                timeToCheckout = (decimal)checkoutDelta.TotalMinutes;
            }

            orders.Add(new OrderTimelinePointDto
            {
                OrderId = row.Id,
                OrderTime = orderTime,
                DeliveryMethod = row.DeliveryMethod,
                RestaurantId = row.RestaurantId,
                WaitTimeMinutes = row.WaitTimeMin,
                TimeToScanMinutes = timeToScan,
                TimeToCheckoutMinutes = timeToCheckout,
                OrderNumber = row.OrderNumber,
                TotalAmount = row.TotalAmountWithVat
            });
        }

        return new OrderTimelineReportDto
        {
            Date = dateUtc,
            Orders = orders
        };
    }

    #endregion
}

