using InnriGreifi.API.Models;

namespace InnriGreifi.API.Models.DTOs;

public class WaitTimeRecordDto
{
    public Guid Id { get; set; }
    public Restaurant Restaurant { get; set; }
    public string RestaurantName => Restaurant.ToString();
    public int? SottMinutes { get; set; }
    public int? SentMinutes { get; set; }
    public bool IsClosed { get; set; }
    public DateTime ScrapedAt { get; set; }
}
