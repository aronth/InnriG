using System.Text.Json;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using OpenAI;
using OpenAI.Chat;

namespace InnriGreifi.API.Services.Steps;

public class OrderVerificationStepHandler : IWorkflowStepHandler
{
    private readonly IGreifinnOrderScraper _orderScraper;
    private readonly IGraphEmailService _graphService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderVerificationStepHandler> _logger;
    private readonly OpenAIClient? _openAIClient;

    public OrderVerificationStepHandler(
        IGreifinnOrderScraper orderScraper,
        IGraphEmailService graphService,
        IConfiguration configuration,
        ILogger<OrderVerificationStepHandler> logger)
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
            _logger.LogInformation("OrderVerificationStepHandler: Verifying order match for workflow {WorkflowInstanceId}", workflow.Id);

            // Get matched orders from previous step
            var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
            if (!workflowData.TryGetValue("MatchedOrders", out var ordersJson) || ordersJson == null)
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "No matched orders found from previous step"
                };
            }

            // Try to extract orders - could be stored as list or as JSON string
            List<GreifinnOrderDto>? orders = null;
            
            if (ordersJson is JsonElement jsonElement)
            {
                // If it's already a JSON array, deserialize directly
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    orders = JsonSerializer.Deserialize<List<GreifinnOrderDto>>(jsonElement.GetRawText());
                }
                // If it's a string (double-encoded), extract and deserialize
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    var jsonString = ExtractJsonString(ordersJson);
                    orders = JsonSerializer.Deserialize<List<GreifinnOrderDto>>(jsonString ?? "[]");
                }
            }
            else if (ordersJson is List<GreifinnOrderDto> directList)
            {
                orders = directList;
            }
            else
            {
                // Fallback: try to extract as string and deserialize
                var jsonString = ExtractJsonString(ordersJson);
                orders = JsonSerializer.Deserialize<List<GreifinnOrderDto>>(jsonString ?? "[]");
            }
            if (orders == null || orders.Count == 0)
            {
                return new WorkflowStepResult
                {
                    Success = true,
                    OutputData = new Dictionary<string, object>
                    {
                        ["SelectedOrderId"] = string.Empty,
                        ["MatchConfidence"] = 0.0m,
                        ["MatchReason"] = "No orders found"
                    },
                    RequiresApproval = false
                };
            }

            // Get email content for comparison
            var firstMessage = conversation.Messages.FirstOrDefault(m => !m.IsAIResponse);
            if (firstMessage == null)
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "No email message found for comparison"
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

            if (string.IsNullOrWhiteSpace(emailText))
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "Could not retrieve email content"
                };
            }

            // If only one order, use it directly (but still verify with AI)
            if (orders.Count == 1)
            {
                var order = orders[0];
                var orderDetail = await _orderScraper.GetOrderDetailAsync(order.OrderId, ct);
                var confidence = await VerifyOrderMatchAsync(emailText, orderDetail, ct);

                return new WorkflowStepResult
                {
                    Success = true,
                    OutputData = new Dictionary<string, object>
                    {
                        ["SelectedOrderId"] = order.OrderId,
                        ["MatchConfidence"] = confidence,
                        ["MatchReason"] = confidence > 0.7m ? "High confidence match" : "Low confidence match"
                    },
                    RequiresApproval = confidence < 0.7m
                };
            }

            // Multiple orders - use AI to select best match
            var bestMatch = await SelectBestOrderMatchAsync(emailText, orders, ct);

            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["SelectedOrderId"] = bestMatch.OrderId,
                    ["MatchConfidence"] = bestMatch.Confidence,
                    ["MatchReason"] = bestMatch.Reason
                },
                RequiresApproval = bestMatch.Confidence < 0.7m || orders.Count > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OrderVerificationStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<decimal> VerifyOrderMatchAsync(string emailText, GreifinnOrderDetailDto orderDetail, CancellationToken ct)
    {
        if (_openAIClient == null)
        {
            _logger.LogWarning("OpenAI client not available, returning default confidence");
            return 0.5m;
        }

        try
        {
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            var orderSummary = $"Order ID: {orderDetail.OrderId}\n" +
                              $"Customer: {orderDetail.CustomerName}\n" +
                              $"Phone: {orderDetail.PhoneNumber}\n" +
                              $"Date: {orderDetail.AddedTime}\n" +
                              $"Total: {orderDetail.TotalPrice}\n" +
                              $"Items: {string.Join(", ", orderDetail.Items.Select(i => $"{i.Name} x{i.Quantity}"))}";

            var systemPrompt = @"You are comparing a customer complaint email with an order to verify if they match.
Analyze the email description and order details to determine how confident you are that this is the correct order.
Respond with ONLY a JSON object: { ""confidence"": 0.0-1.0, ""reason"": ""brief explanation"" }";

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
            var result = ParseConfidenceResponse(content);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying order match with AI");
            return 0.5m;
        }
    }

    private async Task<(string OrderId, decimal Confidence, string Reason)> SelectBestOrderMatchAsync(
        string emailText,
        List<GreifinnOrderDto> orders,
        CancellationToken ct)
    {
        if (_openAIClient == null || orders.Count == 0)
        {
            return (orders.FirstOrDefault()?.OrderId ?? string.Empty, 0.5m, "AI not available");
        }

        try
        {
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            var ordersSummary = string.Join("\n\n", orders.Select((o, i) =>
                $"{i + 1}. Order ID: {o.OrderId}, Customer: {o.CustomerName}, Phone: {o.PhoneNumber}, " +
                $"Date: {o.AddedDate}, Total: {o.TotalPrice}"));

            var systemPrompt = @"You are selecting the best matching order for a customer complaint email.
Analyze the email and compare with the provided orders. Select the order that best matches the complaint.
Respond with ONLY a JSON object: { ""orderIndex"": 1-based index, ""confidence"": 0.0-1.0, ""reason"": ""brief explanation"" }";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Email complaint:\n\n{emailText}\n\n---\n\nAvailable orders:\n\n{ordersSummary}")
            };

            var response = await _openAIClient.GetChatClient(model).CompleteChatAsync(
                chatMessages,
                new ChatCompletionOptions(),
                ct);

            var content = response.Value.Content[0].Text;
            var result = ParseOrderSelectionResponse(content);

            var selectedIndex = result.Item1 - 1;
            if (selectedIndex >= 0 && selectedIndex < orders.Count)
            {
                return (orders[selectedIndex].OrderId, result.Item2, result.Item3);
            }

            return (orders[0].OrderId, 0.5m, "AI selection invalid, using first order");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error selecting best order match with AI");
            return (orders[0].OrderId, 0.5m, "Error in AI selection");
        }
    }

    private decimal ParseConfidenceResponse(string content)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\{[^}]+\}");
            if (jsonMatch.Success)
            {
                var json = jsonMatch.Value;
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("confidence", out var confProp))
                {
                    return (decimal)confProp.GetDouble();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse confidence response: {Content}", content);
        }

        return 0.5m;
    }

    private (int, decimal, string) ParseOrderSelectionResponse(string content)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\{[^}]+\}");
            if (jsonMatch.Success)
            {
                var json = jsonMatch.Value;
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var index = root.TryGetProperty("orderIndex", out var idxProp) ? idxProp.GetInt32() : 1;
                var confidence = root.TryGetProperty("confidence", out var confProp) ? (decimal)confProp.GetDouble() : 0.5m;
                var reason = root.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() ?? "" : "";

                return (index, confidence, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse order selection response: {Content}", content);
        }

        return (1, 0.5m, "Parse error");
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

    private string? ExtractJsonString(object? value)
    {
        if (value == null)
            return null;

        if (value is JsonElement jsonElement)
        {
            // If it's a string JsonElement, get the string value (unescaped)
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                var jsonString = jsonElement.GetString();
                // If the string looks like it might be double-encoded, try to unescape it
                if (!string.IsNullOrEmpty(jsonString) && jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                {
                    try
                    {
                        // Try to unescape - might be double-encoded JSON string
                        return JsonSerializer.Deserialize<string>(jsonString);
                    }
                    catch
                    {
                        // If deserialization fails, just return the string as-is
                        return jsonString;
                    }
                }
                return jsonString;
            }
            // If it's already a JSON object/array, return raw text
            return jsonElement.GetRawText();
        }

        if (value is string stringValue)
        {
            // Check if it's a double-encoded JSON string
            if (stringValue.StartsWith("\"") && stringValue.EndsWith("\"") && stringValue.Length > 2)
            {
                try
                {
                    return JsonSerializer.Deserialize<string>(stringValue);
                }
                catch
                {
                    return stringValue;
                }
            }
            return stringValue;
        }

        return value.ToString();
    }
}

