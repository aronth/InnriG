using System.Text.Json;
using InnriGreifi.API.Models;
using OpenAI;
using OpenAI.Chat;

namespace InnriGreifi.API.Services.Steps;

public class BookingResponseDraftStepHandler : IWorkflowStepHandler
{
    private readonly IGraphEmailService _graphService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingResponseDraftStepHandler> _logger;
    private readonly OpenAIClient? _openAIClient;

    public BookingResponseDraftStepHandler(
        IGraphEmailService graphService,
        IConfiguration configuration,
        ILogger<BookingResponseDraftStepHandler> logger)
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
            _logger.LogInformation("BookingResponseDraftStepHandler: Drafting booking response for workflow {WorkflowInstanceId}", workflow.Id);

            // Get workflow data
            var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
            
            // Get booking request info
            var requestedDate = ExtractString(workflowData, "RequestedDate");
            var requestedTime = extractedData?.RequestedTime?.ToString(@"hh\:mm") ?? ExtractString(workflowData, "RequestedTime");
            var guestCount = extractedData?.GuestCount ?? extractedData?.AdultCount ?? ExtractInt(workflowData, "GuestCount");
            var phoneNumber = extractedData?.ContactPhone ?? ExtractString(workflowData, "PhoneNumber");
            var specialRequests = extractedData?.SpecialRequests ?? ExtractString(workflowData, "SpecialRequests");

            // Get existing bookings info
            var existingBookings = ExtractString(workflowData, "ExistingBookings");
            var bookingCount = ExtractInt(workflowData, "BookingCount") ?? 0;

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

            // Get system prompt from configuration (user can add capacity info)
            var systemPromptOverride = ExtractString(configuration, "SystemPrompt");
            
            // Generate response
            var response = await GenerateResponseAsync(
                emailText,
                requestedDate,
                requestedTime,
                guestCount,
                phoneNumber,
                specialRequests,
                existingBookings,
                bookingCount,
                systemPromptOverride,
                ct);

            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["DraftResponse"] = response,
                    ["BookingDate"] = requestedDate ?? "",
                    ["BookingTime"] = requestedTime ?? "",
                    ["GuestCount"] = guestCount ?? 0,
                    ["PhoneNumber"] = phoneNumber ?? "",
                    ["SpecialRequests"] = specialRequests ?? ""
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BookingResponseDraftStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<string> GenerateResponseAsync(
        string emailText,
        string? requestedDate,
        string? requestedTime,
        int? guestCount,
        string? phoneNumber,
        string? specialRequests,
        string? existingBookingsJson,
        int bookingCount,
        string? systemPromptOverride,
        CancellationToken ct)
    {
        if (_openAIClient == null)
        {
            return "Takk fyrir beiðni um borðabókun. Við munum hafa samband við þig til að staðfesta bókunina.";
        }

        try
        {
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            // Parse existing bookings if available
            var existingBookingsInfo = "";
            if (!string.IsNullOrEmpty(existingBookingsJson))
            {
                try
                {
                    var bookings = JsonSerializer.Deserialize<List<BookingInfo>>(existingBookingsJson);
                    if (bookings != null && bookings.Count > 0)
                    {
                        existingBookingsInfo = "\nNúverandi bókanir á þessum degi:\n";
                        foreach (var booking in bookings)
                        {
                            existingBookingsInfo += $"- {booking.Time}: {booking.GuestCount} gestir\n";
                        }
                    }
                }
                catch
                {
                    // If parsing fails, use raw string
                    existingBookingsInfo = $"\nNúverandi bókanir: {existingBookingsJson}";
                }
            }

            // Build system prompt
            var baseSystemPrompt = @"Þú ert aðstoðarmaður fyrir veitingastað sem svarar beiðnum um borðabókanir.
Skrifaðu vingjarnlegt og faglegt svar á íslensku sem:
1. Takkar fyrir beiðni um bókun
2. Staðfestir eða útskýrir framboð
3. Er stutt og á punkti (2-4 setningar)
4. Er vingjarnlegt en faglegt";

            var systemPrompt = string.IsNullOrEmpty(systemPromptOverride)
                ? baseSystemPrompt
                : $"{baseSystemPrompt}\n\nViðbótarupplýsingar um getu:\n{systemPromptOverride}";

            // Build user message with booking request and existing bookings
            var bookingRequestInfo = "";
            if (!string.IsNullOrEmpty(requestedDate))
                bookingRequestInfo += $"Dagsetning: {requestedDate}\n";
            if (!string.IsNullOrEmpty(requestedTime))
                bookingRequestInfo += $"Tími: {requestedTime}\n";
            if (guestCount.HasValue)
                bookingRequestInfo += $"Fjöldi gesta: {guestCount}\n";
            if (!string.IsNullOrEmpty(phoneNumber))
                bookingRequestInfo += $"Símanúmer: {phoneNumber}\n";
            if (!string.IsNullOrEmpty(specialRequests))
                bookingRequestInfo += $"Sérstakar beiðnir: {specialRequests}\n";

            var userMessage = $"Beiðni um borðabókun:\n\n{emailText}\n\n" +
                            (string.IsNullOrEmpty(bookingRequestInfo) ? "" : $"Upplýsingar um beiðni:\n{bookingRequestInfo}\n") +
                            (string.IsNullOrEmpty(existingBookingsInfo) ? "" : existingBookingsInfo);

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
            _logger.LogWarning(ex, "Error generating booking response with AI");
            return "Takk fyrir beiðni um borðabókun. Við munum hafa samband við þig til að staðfesta bókunina.";
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

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            return jsonElement.GetString();
        if (value is string str) return str;
        return value.ToString();
    }

    private string? ExtractString(object? value)
    {
        if (value == null) return null;
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            return jsonElement.GetString();
        if (value is string str) return str;
        return value.ToString();
    }

    private int? ExtractInt(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Number)
                return jsonElement.GetInt32();
            if (jsonElement.ValueKind == JsonValueKind.String && int.TryParse(jsonElement.GetString(), out var parsedInt))
                return parsedInt;
            return null;
        }

        if (value is int intValue) return intValue;
        if (int.TryParse(value.ToString(), out var parsed)) return parsed;
        return null;
    }

    private class BookingInfo
    {
        public string Time { get; set; } = string.Empty;
        public int GuestCount { get; set; }
    }
}

