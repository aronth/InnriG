using System.ComponentModel.DataAnnotations;

namespace InnriGreifi.API.Models;

public class WaitTimeRecord
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid RestaurantId { get; set; }
    
    public Restaurant Restaurant { get; set; } = null!;
    
    public int? SottMinutes { get; set; }
    
    public int? SentMinutes { get; set; }
    
    public bool IsClosed { get; set; }
    
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}
