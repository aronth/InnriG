using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface IBookingManagementService
{
    Task<List<BookingManagementDto>> GetBookingsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? customerId = null,
        Guid? locationId = null,
        string? status = null);

    Task<BookingManagementDto?> GetBookingByIdAsync(Guid id);
    Task<BookingManagementDto> CreateBookingAsync(CreateBookingDto dto);
    Task<BookingManagementDto?> UpdateBookingAsync(Guid id, UpdateBookingDto dto);
    Task<bool> DeleteBookingAsync(Guid id);
}

