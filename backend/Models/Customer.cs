using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class Customer
{
    public Guid Id { get; set; }

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Email { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public List<Booking> Bookings { get; set; } = new();
}

