namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// Week-by-week growth metrics for orders.
/// </summary>
public class OrderGrowthWeekDto
{
    /// <summary>
    /// Start of the week (Monday).
    /// </summary>
    public DateTime WeekStart { get; set; }
    
    /// <summary>
    /// End of the week (Sunday).
    /// </summary>
    public DateTime WeekEnd { get; set; }
    
    /// <summary>
    /// Total orders in this week.
    /// </summary>
    public int TotalOrders { get; set; }
    
    /// <summary>
    /// Evaluable orders in this week.
    /// </summary>
    public int EvaluableOrders { get; set; }
    
    /// <summary>
    /// Late orders in this week.
    /// </summary>
    public int LateOrders { get; set; }
    
    /// <summary>
    /// Late ratio (0-1).
    /// </summary>
    public decimal LateRatio => EvaluableOrders == 0 ? 0 : (decimal)LateOrders / EvaluableOrders;
    
    /// <summary>
    /// Average wait time in minutes.
    /// </summary>
    public decimal? AvgWaitTimeMin { get; set; }
    
    /// <summary>
    /// P90 wait time in minutes.
    /// </summary>
    public decimal? P90WaitTimeMin { get; set; }
    
    /// <summary>
    /// Growth from previous week (percentage change).
    /// </summary>
    public decimal? TotalOrdersGrowth { get; set; }
    
    public decimal? EvaluableOrdersGrowth { get; set; }
    
    public decimal? LateOrdersGrowth { get; set; }
    
    public decimal? LateRatioGrowth { get; set; }
    
    public decimal? AvgWaitTimeGrowth { get; set; }
    
    public decimal? P90WaitTimeGrowth { get; set; }
}

