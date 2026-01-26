namespace InnriGreifi.API.Models.DTOs;

public class WorkflowStepExecutionDto
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string StepType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object>? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
}

