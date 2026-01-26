namespace InnriGreifi.API.Models.DTOs;

public class BookingWeekDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public List<BookingDayDto> Days { get; set; } = new();
}

