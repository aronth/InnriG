using System.ComponentModel.DataAnnotations;

namespace InnriGreifi.API.Models.DTOs;

public class UpdateMenuItemDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}


