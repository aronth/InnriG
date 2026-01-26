namespace InnriGreifi.API.Services;

public class WorkflowStepResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public Dictionary<string, object> OutputData { get; set; } = new();

    public bool RequiresApproval { get; set; }

    public string? ApprovalPrompt { get; set; }
}

