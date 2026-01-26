namespace InnriGreifi.API.Models.DTOs;

public class WorkflowInstanceDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int CurrentStepIndex { get; set; }
    public Dictionary<string, object>? WorkflowData { get; set; }
    public List<WorkflowStepExecutionDto> StepExecutions { get; set; } = new();
    public List<WorkflowApprovalDto> Approvals { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

