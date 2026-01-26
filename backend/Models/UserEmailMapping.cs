using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class UserEmailMapping
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(300)]
    public string EmailAddress { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? DisplayName { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public User User { get; set; } = null!;
}

