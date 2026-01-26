namespace InnriGreifi.API.Models.DTOs;

public class BookingDayDto
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public List<BookingDto> Bookings { get; set; } = new();
}

