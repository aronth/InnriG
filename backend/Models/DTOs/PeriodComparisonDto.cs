namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// Period-over-period comparison for KPI dashboard.
/// </summary>
public class PeriodComparisonDto
{
    public PeriodInfo CurrentPeriod { get; set; } = new();
    public PeriodInfo PreviousPeriod { get; set; } = new();
    public ComparisonMetrics Comparison { get; set; } = new();
}

public class PeriodInfo
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue => TotalOrders == 0 ? 0 : TotalRevenue / TotalOrders;
    
    public int EvaluableOrders { get; set; }
    public int LateOrders { get; set; }
    public decimal LateRatio => EvaluableOrders == 0 ? 0 : (decimal)LateOrders / EvaluableOrders;
    
    public decimal? AvgWaitTimeMin { get; set; }
    public decimal? P90WaitTimeMin { get; set; }
    
    public Dictionary<string, int> OrdersByMethod { get; set; } = new();
    public Dictionary<string, decimal> RevenueByMethod { get; set; } = new();
    public Dictionary<string, int> OrdersBySource { get; set; } = new();
    public Dictionary<string, decimal> RevenueBySource { get; set; } = new();
    
    public List<DayOfWeekMetrics> ByDayOfWeek { get; set; } = new();
}

public class DayOfWeekMetrics
{
    public int Weekday { get; set; } // 0=Monday..6=Sunday
    public string WeekdayName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public int LateOrders { get; set; }
    public decimal LateRatio { get; set; }
    public decimal? AvgWaitTimeMin { get; set; }
}

public class ComparisonMetrics
{
    public decimal OrdersChangePercent { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public decimal LateRatioChangePercent { get; set; }
    public decimal? WaitTimeChangePercent { get; set; }
    public decimal? P90WaitTimeChangePercent { get; set; }
    
    public Dictionary<string, decimal> OrdersByMethodChangePercent { get; set; } = new();
    public Dictionary<string, decimal> RevenueByMethodChangePercent { get; set; } = new();
}

