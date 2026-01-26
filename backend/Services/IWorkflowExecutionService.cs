using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services;

public interface IWorkflowExecutionService
{
    Task<WorkflowInstance?> InitializeWorkflowAsync(Guid conversationId, string classification, CancellationToken ct = default);

    Task ExecuteWorkflowAsync(Guid workflowInstanceId, CancellationToken ct = default);

    Task<bool> ApproveWorkflowStepAsync(Guid workflowInstanceId, Guid userId, Dictionary<string, object>? approvalData = null, CancellationToken ct = default);

    Task<bool> RejectWorkflowStepAsync(Guid workflowInstanceId, Guid userId, string? reason = null, CancellationToken ct = default);

    Task<WorkflowInstance?> GetWorkflowByConversationAsync(Guid conversationId, CancellationToken ct = default);
}

