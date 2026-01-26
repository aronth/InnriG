namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// Timeline report for visualizing individual orders throughout a single day.
/// Shows time-based metrics for each order on a line graph.
/// </summary>
public class OrderTimelineReportDto
{
    /// <summary>
    /// The date being visualized.
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Individual order data points for the timeline graph.
    /// </summary>
    public List<OrderTimelinePointDto> Orders { get; set; } = new();
}

/// <summary>
/// A single data point representing one order's time metrics.
/// </summary>
public class OrderTimelinePointDto
{
    /// <summary>
    /// Order ID.
    /// </summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Time when the order was created (for X-axis).
    /// </summary>
    public DateTime OrderTime { get; set; }
    
    /// <summary>
    /// Delivery method (Sótt, Sent, Salur, etc.).
    /// </summary>
    public string DeliveryMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// Restaurant/location ID.
    /// </summary>
    public Guid? RestaurantId { get; set; }
    
    /// <summary>
    /// Wait time in minutes (time from order creation to ready time).
    /// Null if not calculable.
    /// </summary>
    public int? WaitTimeMinutes { get; set; }
    
    /// <summary>
    /// Time to scan in minutes (time from order creation to when scanned).
    /// Null if not scanned or not calculable.
    /// </summary>
    public decimal? TimeToScanMinutes { get; set; }
    
    /// <summary>
    /// Time to checkout in minutes (time from order creation to when checked out).
    /// Only applicable for delivery orders. Null if not checked out or not calculable.
    /// </summary>
    public decimal? TimeToCheckoutMinutes { get; set; }
    
    /// <summary>
    /// Order number for identification/tooltips.
    /// </summary>
    public string? OrderNumber { get; set; }
    
    /// <summary>
    /// Total amount with VAT for reference.
    /// </summary>
    public decimal? TotalAmount { get; set; }
}

