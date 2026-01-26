using System.Text.Json;
using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services.Steps;

public class CreditIssuanceStepHandler : IWorkflowStepHandler
{
    private readonly ILogger<CreditIssuanceStepHandler> _logger;

    public CreditIssuanceStepHandler(ILogger<CreditIssuanceStepHandler> logger)
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
        try
        {
            _logger.LogInformation("CreditIssuanceStepHandler: Issuing credit for workflow {WorkflowInstanceId}", workflow.Id);

            // Get workflow data
            var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
            
            var orderId = ExtractString(workflowData, "SelectedOrderId") ?? "";
            var creditAmount = ExtractDecimal(workflowData, "ProposedCreditAmount") ?? 0m;
            var phoneNumber = extractedData?.ContactPhone ?? "";

            // TODO: Integrate with actual credit issuance API/system
            // For now, just log the action
            _logger.LogInformation(
                "Credit issuance (placeholder): Order {OrderId}, Amount {Amount:N0} kr., Phone {Phone}",
                orderId, creditAmount, phoneNumber);

            return Task.FromResult(new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["CreditIssued"] = true,
                    ["CreditIssuedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreditIssuanceStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
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

    private decimal? ExtractDecimal(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Number)
                return jsonElement.GetDecimal();
            if (jsonElement.ValueKind == JsonValueKind.String && decimal.TryParse(jsonElement.GetString(), out var parsed))
                return parsed;
            return null;
        }

        if (value is decimal d)
            return d;

        if (decimal.TryParse(value.ToString(), out var parsedValue))
            return parsedValue;

        return null;
    }
}

