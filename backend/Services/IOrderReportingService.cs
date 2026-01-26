using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface IOrderReportingService
{
    Task<OrderReportSummaryDto> GetSummaryAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<OrderReportSummaryDto> GetCompleteSummaryAsync(
        CancellationToken ct = default);

    Task<List<OrderWaitTimeSeriesPointDto>> GetWaitTimeSeriesAsync(
        DateTime? from,
        DateTime? to,
        string granularity = "day",
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<OrderWaitTimeDistributionDto> GetWaitTimeDistributionAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        int bucketSize = 5,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<List<OrderLateRatioPointDto>> GetLateSeriesAsync(
        DateTime? from,
        DateTime? to,
        string granularity = "day",
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<OrderHeatmapDto> GetVolumeHeatmapAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<OrderHeatmapDto> GetLateHeatmapAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<OrderHeatmapDto> GetWaitTimeHeatmapAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<OrderHeatmapDto> GetP90WaitTimeHeatmapAsync(
        DateTime? from,
        DateTime? to,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<OrderVolumeExpectedIndexDto> GetVolumeExpectedIndexAsync(
        DateTime from,
        DateTime to,
        int baselineWindowDays = 56,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<List<OrderGrowthWeekDto>> GetGrowthByWeekAsync(
        DateTime from,
        DateTime to,
        string? deliveryMethod = null,
        int? weekday = null,
        bool? isLate = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<PeriodComparisonDto> GetPeriodComparisonAsync(
        string periodType,
        DateTime periodStart,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);

    Task<ForecastDto> GetForecastAsync(
        string periodType,
        DateTime targetPeriodStart,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        int lookbackDays = 56,
        CancellationToken ct = default);

    Task<OrderTimelineReportDto> GetTimelineAsync(
        DateTime date,
        string? deliveryMethod = null,
        Guid? restaurantId = null,
        CancellationToken ct = default);
}

