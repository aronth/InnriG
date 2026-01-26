namespace InnriGreifi.API.Models.DTOs;

public class BookingDto
{
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string? EndTime { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public string? LocationCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public bool NeedsPrint { get; set; }
}

