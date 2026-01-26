namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// Forecast/prediction for next period based on historical trends.
/// </summary>
public class ForecastDto
{
    public DateTime ForecastPeriodStart { get; set; }
    public DateTime ForecastPeriodEnd { get; set; }
    
    public int PredictedOrders { get; set; }
    public int PredictedOrdersMin { get; set; }
    public int PredictedOrdersMax { get; set; }
    
    public decimal PredictedRevenue { get; set; }
    public decimal PredictedRevenueMin { get; set; }
    public decimal PredictedRevenueMax { get; set; }
    
    public decimal PredictedLateRatio { get; set; }
    public decimal? PredictedAvgWaitTime { get; set; }
    public decimal? PredictedP90WaitTime { get; set; }
    
    /// <summary>
    /// Trend direction: "improving", "declining", "stable"
    /// </summary>
    public string TrendDirection { get; set; } = "stable";
    
    /// <summary>
    /// Trend strength: 0-100 (how confident we are in the trend)
    /// </summary>
    public decimal TrendStrength { get; set; }
    
    /// <summary>
    /// Number of days used for baseline calculation.
    /// </summary>
    public int BaselineWindowDays { get; set; }
    
    /// <summary>
    /// Breakdown by weekday for the forecast period.
    /// </summary>
    public List<ForecastWeekdayDto> ByWeekday { get; set; } = new();
}

public class ForecastWeekdayDto
{
    public int Weekday { get; set; } // 0=Monday..6=Sunday
    public string WeekdayName { get; set; } = string.Empty;
    public int DaysInPeriod { get; set; }
    public int PredictedOrders { get; set; }
    public decimal PredictedRevenue { get; set; }
}

