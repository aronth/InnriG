namespace InnriGreifi.API.Models.DTOs;

public class ApproveWorkflowRequest
{
    public Dictionary<string, object>? ApprovalData { get; set; }
    public string? Comments { get; set; }
}

