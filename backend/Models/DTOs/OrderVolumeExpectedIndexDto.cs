namespace InnriGreifi.API.Models.DTOs;

/// <summary>
/// Volume comparison using weekday expected-index normalization.
/// </summary>
public class OrderVolumeExpectedIndexDto
{
    /// <summary>
    /// Start of the target period being analyzed.
    /// </summary>
    public DateTime PeriodStart { get; set; }
    
    /// <summary>
    /// End of the target period being analyzed.
    /// </summary>
    public DateTime PeriodEnd { get; set; }
    
    /// <summary>
    /// Number of days used for baseline calculation.
    /// </summary>
    public int BaselineWindowDays { get; set; }
    
    /// <summary>
    /// Actual order count in the target period.
    /// </summary>
    public int ActualTotal { get; set; }
    
    /// <summary>
    /// Expected order count based on weekday baseline.
    /// </summary>
    public decimal ExpectedTotal { get; set; }
    
    /// <summary>
    /// Index = Actual / Expected. > 1 means busier than expected.
    /// </summary>
    public decimal Index => ExpectedTotal == 0 ? 0 : ActualTotal / ExpectedTotal;
    
    /// <summary>
    /// Percentage difference from expected: (Actual - Expected) / Expected * 100.
    /// </summary>
    public decimal PercentDiff => ExpectedTotal == 0 ? 0 : (ActualTotal - ExpectedTotal) / ExpectedTotal * 100;
    
    /// <summary>
    /// Per-weekday breakdown (0=Monday..6=Sunday).
    /// </summary>
    public List<WeekdayExpectedIndexDto> ByWeekday { get; set; } = new();
}

public class WeekdayExpectedIndexDto
{
    public int Weekday { get; set; } // 0=Monday..6=Sunday
    public string WeekdayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of this weekday in the target period.
    /// </summary>
    public int DaysInPeriod { get; set; }
    
    /// <summary>
    /// Actual orders on this weekday in the target period.
    /// </summary>
    public int ActualCount { get; set; }
    
    /// <summary>
    /// Average daily volume for this weekday from baseline.
    /// </summary>
    public decimal BaselineAvgPerDay { get; set; }
    
    /// <summary>
    /// Expected = BaselineAvgPerDay * DaysInPeriod.
    /// </summary>
    public decimal ExpectedCount => BaselineAvgPerDay * DaysInPeriod;
    
    /// <summary>
    /// Index = Actual / Expected.
    /// </summary>
    public decimal Index => ExpectedCount == 0 ? 0 : ActualCount / ExpectedCount;
}

