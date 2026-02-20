using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class Booking
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? LocationId { get; set; } // Restaurant/Location

    public DateTime BookingDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public int AdultCount { get; set; }

    public int ChildCount { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Ný"; // Ný, Staðfest, Afbókað, Situr, Farinn

    [MaxLength(2000)]
    public string? SpecialRequests { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public bool NeedsPrint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public Customer Customer { get; set; } = null!;

    [JsonIgnore]
    public Restaurant? Location { get; set; }

    [JsonIgnore]
    public List<BookingMenuItem> BookingMenuItems { get; set; } = new();
}

