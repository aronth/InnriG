using System.ComponentModel.DataAnnotations;

namespace InnriGreifi.API.Models;

public class EmailJunkFilter
{
    public Guid Id { get; set; }

    [MaxLength(500)]
    public string? Subject { get; set; } // Null means match any subject

    [MaxLength(300)]
    public string? SenderEmail { get; set; } // Null means match any sender

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

