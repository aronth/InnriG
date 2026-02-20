using System.ComponentModel.DataAnnotations;

namespace InnriGreifi.API.Models.DTOs;

public class UpdateMenuDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ForWho { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}


