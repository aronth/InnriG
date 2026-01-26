namespace InnriGreifi.API.Models.DTOs;

public class RemainingTimeAggregationDto
{
    public DateOnly Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public List<TimeIntervalData> Intervals { get; set; } = new();
}

public class TimeIntervalData
{
    public string TimeLabel { get; set; } = string.Empty; // e.g., "10:00", "10:15"
    public DateTime IntervalStart { get; set; }
    public double? AverageRemainingMinutes { get; set; } // null if no data
    public int OrderCount { get; set; }
}

