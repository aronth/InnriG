using System.Text.Json;
using InnriGreifi.API.Models;
using OpenAI;
using OpenAI.Chat;

namespace InnriGreifi.API.Services.Steps;

public class ResponseDraftStepHandler : IWorkflowStepHandler
{
    private readonly IGraphEmailService _graphService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResponseDraftStepHandler> _logger;
    private readonly OpenAIClient? _openAIClient;

    public ResponseDraftStepHandler(
        IGraphEmailService graphService,
        IConfiguration configuration,
        ILogger<ResponseDraftStepHandler> logger)
    {
        _graphService = graphService;
        _configuration = configuration;
        _logger = logger;

        var apiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _openAIClient = new OpenAIClient(apiKey);
        }
    }

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("ResponseDraftStepHandler: Drafting response for workflow {WorkflowInstanceId}", workflow.Id);

            // Get workflow data
            var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
            var creditAmount = ExtractDecimal(workflowData, "ProposedCreditAmount") ?? 0m;

            // Get email content for context
            var firstMessage = conversation.Messages.FirstOrDefault(m => !m.IsAIResponse);
            if (firstMessage == null)
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "No email message found"
                };
            }

            string emailText;
            if (string.Equals(firstMessage.FromEmail, "ai@system.local", StringComparison.OrdinalIgnoreCase))
            {
                emailText = firstMessage.MessageBody ?? string.Empty;
            }
            else
            {
                var messageBody = await _graphService.GetMessageBodyAsync(firstMessage.GraphMessageId, ct);
                emailText = messageBody?.Text ?? messageBody?.Html ?? string.Empty;
            }

            // Generate response
            var response = await GenerateResponseAsync(emailText, creditAmount, extractedData, ct);

            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["DraftResponse"] = response
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResponseDraftStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<string> GenerateResponseAsync(
        string emailText,
        decimal creditAmount,
        EmailExtractedData? extractedData,
        CancellationToken ct)
    {
        if (_openAIClient == null)
        {
            return "Við biðjumst afsökunar á óþægindunum. Við höfum úthlutað inneign upp á " +
                   $"{creditAmount:N0} kr. á símanúmerið þitt.";
        }

        try
        {
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            var customerInfo = "";
            if (extractedData != null)
            {
                if (!string.IsNullOrEmpty(extractedData.ContactName))
                    customerInfo += $"Nafn: {extractedData.ContactName}\n";
                if (!string.IsNullOrEmpty(extractedData.ContactPhone))
                    customerInfo += $"Sími: {extractedData.ContactPhone}\n";
            }

            var systemPrompt = @"Þú ert aðstoðarmaður fyrir veitingastað sem svarar kvörtunum viðskiptavina.
Skrifaðu vingjarnlegt og faglegt svar á íslensku sem:
1. Biðurst afsökunar á óþægindunum
2. Útskýrir að inneign hefur verið úthlutað
3. Gefur upp inneignarupphæð
4. Er stutt og á punkti (2-4 setningar)
5. Er vingjarnlegt en faglegt";

            var userMessage = $"Kvörtun viðskiptavinar:\n\n{emailText}\n\n" +
                            $"Inneignarupphæð: {creditAmount:N0} kr.\n\n" +
                            (string.IsNullOrEmpty(customerInfo) ? "" : $"Upplýsingar:\n{customerInfo}");

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var response = await _openAIClient.GetChatClient(model).CompleteChatAsync(
                chatMessages,
                new ChatCompletionOptions(),
                ct);

            return response.Value.Content[0].Text?.Trim() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating response with AI");
            return "Við biðjumst afsökunar á óþægindunum. Við höfum úthlutað inneign upp á " +
                   $"{creditAmount:N0} kr. á símanúmerið þitt.";
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

