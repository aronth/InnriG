using System.Text.Json;
using System.Text.RegularExpressions;
using InnriGreifi.API.Models;
using OpenAI;
using OpenAI.Chat;

namespace InnriGreifi.API.Services.Steps;

public class CreditCalculationStepHandler : IWorkflowStepHandler
{
    private readonly IGreifinnOrderScraper _orderScraper;
    private readonly IGraphEmailService _graphService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreditCalculationStepHandler> _logger;
    private readonly OpenAIClient? _openAIClient;

    public CreditCalculationStepHandler(
        IGreifinnOrderScraper orderScraper,
        IGraphEmailService graphService,
        IConfiguration configuration,
        ILogger<CreditCalculationStepHandler> logger)
    {
        _orderScraper = orderScraper;
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
            _logger.LogInformation("CreditCalculationStepHandler: Calculating credit amount for workflow {WorkflowInstanceId}", workflow.Id);

            // Get selected order
            var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
            if (!workflowData.TryGetValue("SelectedOrderId", out var orderIdObj) || orderIdObj == null)
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "No order selected from previous step"
                };
            }

            var orderId = ExtractString(orderIdObj) ?? string.Empty;
            if (string.IsNullOrEmpty(orderId))
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "Invalid order ID"
                };
            }

            // Get order details
            var orderDetail = await _orderScraper.GetOrderDetailAsync(orderId, ct);

            // Get email content
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

            // Extract credit amount using AI
            var creditAmount = await ExtractCreditAmountAsync(emailText, orderDetail, ct);

            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["ProposedCreditAmount"] = creditAmount,
                    ["OrderTotal"] = orderDetail.TotalPrice
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreditCalculationStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<decimal> ExtractCreditAmountAsync(string emailText, Models.DTOs.GreifinnOrderDetailDto orderDetail, CancellationToken ct)
    {
        if (_openAIClient == null)
        {
            _logger.LogWarning("OpenAI client not available, using order total as credit");
            return orderDetail.TotalPrice;
        }

        try
        {
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            var systemPrompt = @"You are extracting the credit amount requested in a customer complaint email.
The customer is complaining about an order issue and may request a credit/refund.
Extract the specific amount mentioned, or if no amount is specified, suggest an appropriate credit based on the complaint.
Respond with ONLY a JSON object: { ""amount"": decimal number, ""reason"": ""brief explanation"" }";

            var orderSummary = $"Order Total: {orderDetail.TotalPrice}\n" +
                              $"Items: {string.Join(", ", orderDetail.Items.Select(i => $"{i.Name} x{i.Quantity}"))}";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Email complaint:\n\n{emailText}\n\n---\n\nOrder details:\n\n{orderSummary}")
            };

            var response = await _openAIClient.GetChatClient(model).CompleteChatAsync(
                chatMessages,
                new ChatCompletionOptions(),
                ct);

            var content = response.Value.Content[0].Text;
            var result = ParseCreditAmountResponse(content);

            // Cap credit at order total
            return Math.Min(result, orderDetail.TotalPrice);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting credit amount with AI, using order total");
            return orderDetail.TotalPrice;
        }
    }

    private decimal ParseCreditAmountResponse(string content)
    {
        try
        {
            var jsonMatch = Regex.Match(content, @"\{[^}]+\}");
            if (jsonMatch.Success)
            {
                var json = jsonMatch.Value;
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("amount", out var amountProp))
                {
                    return (decimal)amountProp.GetDouble();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse credit amount response: {Content}", content);
        }

        return 0m;
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

    private string? ExtractString(object? value)
    {
        if (value == null)
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

