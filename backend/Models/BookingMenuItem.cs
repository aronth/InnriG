using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class BookingMenuItem
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid MenuItemId { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; } // Price at time of booking

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public Booking Booking { get; set; } = null!;

    [JsonIgnore]
    public MenuItem MenuItem { get; set; } = null!;
}

