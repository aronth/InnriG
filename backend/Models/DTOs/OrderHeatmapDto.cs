namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// A heatmap matrix for weekday × hour-of-day analysis.
/// Weekday: 0=Monday, 6=Sunday (ISO week).
/// Hour: 0-23.
/// </summary>
public class OrderHeatmapDto
{
    /// <summary>
    /// Type of data in the heatmap: "volume", "lateRatio", "waitTime", or "p90WaitTime".
    /// </summary>
    public string DataType { get; set; } = "volume";
    
    /// <summary>
    /// Cells indexed by [weekday][hour], where weekday 0=Monday..6=Sunday.
    /// </summary>
    public List<List<OrderHeatmapCellDto>> Cells { get; set; } = new();
    
    /// <summary>
    /// Weekday labels (Monday-Sunday).
    /// </summary>
    public List<string> WeekdayLabels { get; set; } = new()
    {
        "Mánudagur", "Þriðjudagur", "Miðvikudagur", "Fimmtudagur", 
        "Föstudagur", "Laugardagur", "Sunnudagur"
    };
    
    /// <summary>
    /// Hour labels (0-23).
    /// </summary>
    public List<string> HourLabels { get; set; } = Enumerable.Range(0, 24)
        .Select(h => $"{h:00}:00")
        .ToList();
}

public class OrderHeatmapCellDto
{
    public int Weekday { get; set; } // 0=Monday..6=Sunday
    public int Hour { get; set; } // 0-23
    
    /// <summary>
    /// For volume heatmap: count of orders.
    /// For late ratio heatmap: late ratio (0-1).
    /// For wait time heatmap: average wait time in minutes.
    /// For P90 wait time heatmap: 90th percentile wait time in minutes.
    /// </summary>
    public decimal Value { get; set; }
    
    /// <summary>
    /// Additional context (e.g., for late ratio: total/evaluable/late counts).
    /// </summary>
    public int? Total { get; set; }
    public int? Evaluable { get; set; }
    public int? LateCount { get; set; }
}

