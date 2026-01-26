namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// High-level KPIs for the orders report summary.
/// </summary>
public class OrderReportSummaryDto
{
    public int TotalOrders { get; set; }
    public int EvaluableOrders { get; set; }
    public int LateOrders { get; set; }
    public decimal LateRatio => EvaluableOrders == 0 ? 0 : (decimal)LateOrders / EvaluableOrders;
    
    public decimal? AvgWaitTimeMin { get; set; }
    public decimal? P90WaitTimeMin { get; set; }
    
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue => TotalOrders == 0 ? 0 : TotalRevenue / TotalOrders;
    
    public Dictionary<string, int> OrdersByMethod { get; set; } = new();
    public Dictionary<string, decimal> RevenueByMethod { get; set; } = new();
    public Dictionary<string, int> OrdersBySource { get; set; } = new();
    public Dictionary<string, decimal> RevenueBySource { get; set; } = new();
}

