using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class WorkflowApproval
{
    public Guid Id { get; set; }

    public Guid WorkflowInstanceId { get; set; }

    public Guid? StepExecutionId { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [MaxLength(2000)]
    public string? Comments { get; set; }

    public string? ApprovalDataJson { get; set; } // Additional approval data (edited values, etc.)

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public WorkflowInstance WorkflowInstance { get; set; } = null!;

    [JsonIgnore]
    public User? ApprovedBy { get; set; }
}

