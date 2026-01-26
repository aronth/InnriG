using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services;

public interface IWorkflowStepHandler
{
    Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default);
}

