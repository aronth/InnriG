using InnriGreifi.API.Models;

namespace InnriGreifi.API.Models.DTOs;

public class WaitTimeResultDto
{
    public Restaurant Restaurant { get; set; } = null!;
    public string RestaurantName => Restaurant?.Name ?? "Unknown";
    public int? SottMinutes { get; set; }
    public int? SentMinutes { get; set; }
    public bool IsClosed { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
