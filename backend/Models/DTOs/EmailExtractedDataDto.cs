namespace InnriGreifi.API.Models.DTOs;

public class EmailExtractedDataDto
{
    public Guid Id { get; set; }
    public string Classification { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public DateTime? RequestedDate { get; set; }
    public TimeSpan? RequestedTime { get; set; }
    public int? GuestCount { get; set; }
    public int? AdultCount { get; set; }
    public int? ChildCount { get; set; }
    public string? LocationCode { get; set; }
    public string? SpecialRequests { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactName { get; set; }
    public DateTime ExtractedAt { get; set; }
}

