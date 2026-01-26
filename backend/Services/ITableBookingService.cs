using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface ITableBookingService
{
    Task<TableBookingListDto> GetBookingsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? contactName = null,
        string? contactPhone = null,
        int? statusId = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}

