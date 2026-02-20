using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class UserEmailToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(300)]
    public string EmailAddress { get; set; } = string.Empty;

    public string EncryptedRefreshToken { get; set; } = string.Empty;

    // Access tokens can be very long (JWT tokens can be 2000+ characters)
    // Using text type in database (no max length restriction)
    public string? AccessToken { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastRefreshedAt { get; set; }

    public bool IsSystemInbox { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public User User { get; set; } = null!;
}


