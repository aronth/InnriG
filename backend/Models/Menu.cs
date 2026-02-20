using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class Menu
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ForWho { get; set; } = string.Empty; // Target group (e.g., "Corporate", "Private", "All")

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public List<MenuItem> MenuItems { get; set; } = new();
}


