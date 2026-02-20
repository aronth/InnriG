using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class MenuItem
{
    public Guid Id { get; set; }

    public Guid MenuId { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty; // e.g., "Pizza Buffet - Adult", "Súpa dagsins"

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public Menu Menu { get; set; } = null!;

    // BookingMenuItems navigation will be added in Phase 4
    // [JsonIgnore]
    // public List<BookingMenuItem> BookingMenuItems { get; set; } = new();
}

