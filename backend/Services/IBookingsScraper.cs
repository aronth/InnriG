using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface IBookingsScraper
{
    Task<BookingWeekDto> ScrapeWeekAsync(long unixTimestamp);
}

