namespace InnriGreifi.API.Models.DTOs;

public class KPIsDto
{
    public KPIValue ProductListCompleteness { get; set; } = null!;
    public KPIValue StaleProducts { get; set; } = null!;
    public KPIValue QuarterlyUpdateFrequency { get; set; } = null!;
    public KPIValue UsageMetrics { get; set; } = null!;
    public QuarterlyBreakdownDto QuarterlyBreakdown { get; set; } = null!;
}

public class KPIValue
{
    public decimal Current { get; set; }
    public decimal Target { get; set; }
    public string Status { get; set; } = string.Empty; // "Met", "Ekki metið", "Árangur"
    public string Details { get; set; } = string.Empty;
}

public class QuarterlyBreakdownDto
{
    public QuarterStatusDto Q1 { get; set; } = null!;
    public QuarterStatusDto Q2 { get; set; } = null!;
    public QuarterStatusDto Q3 { get; set; } = null!;
    public QuarterStatusDto Q4 { get; set; } = null!;
}

public class QuarterStatusDto
{
    public int Updated { get; set; }
    public int Total { get; set; }
    public decimal Percentage { get; set; }
}

