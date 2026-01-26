namespace InnriGreifi.API.Models.DTOs;

public class WorkflowApprovalDto
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid? StepExecutionId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public Dictionary<string, object>? ApprovalData { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

