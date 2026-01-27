namespace InnriGreifi.API.Models.DTOs;

public class TimeReportParseResultDto
{
    public List<EmployeeTimeReportDto> Employees { get; set; } = new();
    public int TotalShifts { get; set; }
    public DateTime? ReportStartDate { get; set; }
    public DateTime? ReportEndDate { get; set; }
}

public class EmployeeTimeReportDto
{
    public string Name { get; set; } = string.Empty;
    public string Kennitala { get; set; } = string.Empty;
    public List<ShiftDto> Shifts { get; set; } = new();
    public int TotalShifts => Shifts.Count;
    public TimeSpan? TotalHours { get; set; }
}

public class ShiftDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public string? WorkLocation { get; set; }
    public string? Type { get; set; }
    public string? Approved { get; set; }
    public string? ClockInNote { get; set; }
    public string? DayNote { get; set; }
    public string? GroupNumber { get; set; }
    public string? Group { get; set; }
}

