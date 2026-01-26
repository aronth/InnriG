using System.Text.Json;
using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services.Steps;

public class ApprovalStepHandler : IWorkflowStepHandler
{
    private readonly ILogger<ApprovalStepHandler> _logger;

    public ApprovalStepHandler(ILogger<ApprovalStepHandler> logger)
    {
        _logger = logger;
    }

    public Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default)
    {
        _logger.LogInformation("ApprovalStepHandler: Creating approval gate for workflow {WorkflowInstanceId}", workflow.Id);

        // Get workflow data for approval prompt
        var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
        
        var approvalPrompt = BuildApprovalPrompt(workflowData);

        return Task.FromResult(new WorkflowStepResult
        {
            Success = true,
            RequiresApproval = true,
            ApprovalPrompt = approvalPrompt,
            OutputData = new Dictionary<string, object>()
        });
    }

    private string BuildApprovalPrompt(Dictionary<string, object> workflowData)
    {
        var prompt = "Please review and approve the following:\n\n";

        if (workflowData.TryGetValue("SelectedOrderId", out var orderId))
        {
            prompt += $"Selected Order: {orderId}\n";
        }

        if (workflowData.TryGetValue("MatchConfidence", out var confidence))
        {
            prompt += $"Match Confidence: {confidence}\n";
        }

        if (workflowData.TryGetValue("ProposedCreditAmount", out var amount))
        {
            prompt += $"Proposed Credit Amount: {amount:N0} kr.\n";
        }

        if (workflowData.TryGetValue("DraftResponse", out var response))
        {
            prompt += $"\nDraft Response:\n{response}\n";
        }

        return prompt;
    }

    private Dictionary<string, object> DeserializeWorkflowData(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}

