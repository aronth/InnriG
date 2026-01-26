using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace InnriGreifi.API.Services;

public interface IOrderImportService
{
    Task<OrderImportResultDto> ImportAsync(IFormFile file, Guid? restaurantId, CancellationToken cancellationToken = default);
}


