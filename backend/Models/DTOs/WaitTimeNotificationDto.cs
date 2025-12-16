using InnriGreifi.API.Models;

namespace InnriGreifi.API.Models.DTOs;

public class WaitTimeNotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Restaurant Restaurant { get; set; }
    public string RestaurantName => Restaurant.ToString();
    public int? SottThresholdMinutes { get; set; }
    public int? SentThresholdMinutes { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastNotifiedSott { get; set; }
    public DateTime? LastNotifiedSent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateWaitTimeNotificationDto
{
    public Restaurant Restaurant { get; set; }
    public string PushoverUserKey { get; set; } = string.Empty;
    public int? SottThresholdMinutes { get; set; }
    public int? SentThresholdMinutes { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateWaitTimeNotificationDto
{
    public string? PushoverUserKey { get; set; }
    public int? SottThresholdMinutes { get; set; }
    public int? SentThresholdMinutes { get; set; }
    public bool? IsEnabled { get; set; }
}
