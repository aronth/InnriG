using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface IWaitTimeScraper
{
    Task<WaitTimeResultDto> ScrapeGreifinnAsync();
    Task<WaitTimeResultDto> ScrapeSpretturinnAsync();
    Task<WaitTimeResultDto> ScrapeAsync(Restaurant restaurant);
}
