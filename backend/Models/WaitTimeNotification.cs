using System.ComponentModel.DataAnnotations;

namespace InnriGreifi.API.Models;

public class WaitTimeNotification
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Restaurant Restaurant { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string PushoverUserKey { get; set; } = string.Empty;
    
    public int? SottThresholdMinutes { get; set; }
    
    public int? SentThresholdMinutes { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime? LastNotifiedSott { get; set; }
    
    public DateTime? LastNotifiedSent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
