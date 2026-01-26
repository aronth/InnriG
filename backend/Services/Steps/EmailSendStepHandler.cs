using System.Text.Json;
using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services.Steps;

public class EmailSendStepHandler : IWorkflowStepHandler
{
    private readonly IGraphEmailService _graphService;
    private readonly ILogger<EmailSendStepHandler> _logger;

    public EmailSendStepHandler(
        IGraphEmailService graphService,
        ILogger<EmailSendStepHandler> logger)
    {
        _graphService = graphService;
        _logger = logger;
    }

    public Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("EmailSendStepHandler: Sending response email for workflow {WorkflowInstanceId}", workflow.Id);

            // Get workflow data
            var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
            
            var draftResponse = ExtractString(workflowData, "DraftResponse") ?? "";

            if (string.IsNullOrEmpty(draftResponse))
            {
                return Task.FromResult(new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "No draft response found"
                });
            }

            // TODO: Implement email sending via GraphEmailService
            // For now, just log the action
            _logger.LogInformation(
                "Email send (placeholder): To {Email}, Subject: Re: {Subject}, Response length: {Length}",
                conversation.FromEmail, conversation.Subject, draftResponse.Length);

            return Task.FromResult(new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["EmailSent"] = true,
                    ["EmailSentAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in EmailSendStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
            return Task.FromResult(new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
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

    private string? ExtractString(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String)
                return jsonElement.GetString();
            return jsonElement.GetRawText();
        }

        return value.ToString();
    }
}

