using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class WorkflowInstance
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    [MaxLength(100)]
    public string WorkflowType { get; set; } = string.Empty; // e.g., "CreditIssuance", "TableBooking"

    [MaxLength(50)]
    public string State { get; set; } = "Pending"; // Pending, InProgress, AwaitingApproval, Completed, Failed

    public int CurrentStepIndex { get; set; } = 0;

    public string? WorkflowDataJson { get; set; } // Flexible JSON storage for workflow-specific data

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public EmailConversation Conversation { get; set; } = null!;

    [JsonIgnore]
    public List<WorkflowStepExecution> StepExecutions { get; set; } = new();

    [JsonIgnore]
    public List<WorkflowApproval> Approvals { get; set; } = new();
}

