using System.ComponentModel.DataAnnotations;

namespace InnriGreifi.API.Models;

public class EmailClassification
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? SystemPrompt { get; set; }

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<WorkflowDefinition> Workflows { get; set; } = new();
}

