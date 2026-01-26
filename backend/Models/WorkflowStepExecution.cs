using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class WorkflowStepExecution
{
    public Guid Id { get; set; }

    public Guid WorkflowInstanceId { get; set; }

    [MaxLength(100)]
    public string StepType { get; set; } = string.Empty; // e.g., "OrderLookup", "Approval"

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed, Skipped

    public string? ResultJson { get; set; } // Step output data as JSON

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
}

