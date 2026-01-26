namespace InnriGreifi.API.Models.DTOs;

public class TableBookingDto
{
    public string BookingId { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public int? GuestCount { get; set; }
    public string? Status { get; set; } // Ný/Afbókað/Situr/Farinn
    public bool HasComment { get; set; } // Athugasemd
    public string? DetailUrl { get; set; }
}

public class TableBookingListDto
{
    public List<TableBookingDto> Bookings { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMorePages { get; set; }
}

