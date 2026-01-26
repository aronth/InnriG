namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// A single point in a wait-time series (e.g., per day/week/month).
/// </summary>
public class OrderWaitTimeSeriesPointDto
{
    public DateTime PeriodStart { get; set; }
    public int Count { get; set; }
    public decimal? AvgWaitTimeMin { get; set; }
    public decimal? P90WaitTimeMin { get; set; }
}

