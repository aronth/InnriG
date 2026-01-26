using System.Text.Json;
using System.Text.RegularExpressions;
using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;

namespace InnriGreifi.API.Services;

public class EmailClassificationService : IEmailClassificationService
{
    private readonly IGraphEmailService _graphService;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailClassificationService> _logger;
    private readonly OpenAIClient? _openAIClient;

    public EmailClassificationService(
        IGraphEmailService graphService,
        AppDbContext context,
        IConfiguration configuration,
        ILogger<EmailClassificationService> logger)
    {
        _graphService = graphService;
        _context = context;
        _configuration = configuration;
        _logger = logger;

        var apiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _openAIClient = new OpenAIClient(apiKey);
        }
        else
        {
            _logger.LogWarning("OpenAI API key not configured. Classification will be disabled.");
        }
    }

    public async Task<ClassificationResult> ClassifyEmailAsync(string messageId, CancellationToken ct = default)
    {
        if (_openAIClient == null)
        {
            _logger.LogWarning("OpenAI client not available, returning default classification");
            return new ClassificationResult
            {
                Classification = "GeneralInquiry",
                Confidence = 0.5m,
                ExtractedData = null
            };
        }

        try
        {
            _logger.LogInformation("Starting AI classification for message {MessageId}", messageId);

            // Get message from database to find conversation
            var message = await _context.EmailMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.GraphMessageId == messageId, ct);

            if (message == null)
            {
                _logger.LogWarning("Message {MessageId} not found in database", messageId);
                throw new InvalidOperationException($"Message {messageId} not found");
            }

            _logger.LogInformation("Processing email from {FromEmail} in conversation {ConversationId}", 
                message.FromEmail, message.ConversationId);

            string emailText;
            
            // If message is from ai@system.local, fetch body from database
            if (string.Equals(message.FromEmail, "ai@system.local", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(message.MessageBody))
                {
                    _logger.LogWarning("Message body is empty for AI message {MessageId}", messageId);
                    return new ClassificationResult
                    {
                        Classification = "GeneralInquiry",
                        Confidence = 0.5m,
                        ExtractedData = null
                    };
                }
                emailText = message.MessageBody;
            }
            else
            {
                // Fetch message body from Graph API
                var messageBody = await _graphService.GetMessageBodyAsync(messageId, ct);
                if (messageBody == null)
                {
                    _logger.LogWarning("Could not fetch message body for {MessageId}", messageId);
                    return new ClassificationResult
                    {
                        Classification = "GeneralInquiry",
                        Confidence = 0.5m,
                        ExtractedData = null
                    };
                }

                emailText = messageBody.Text ?? messageBody.Html ?? string.Empty;
            }
            if (string.IsNullOrWhiteSpace(emailText))
            {
                _logger.LogWarning("Message body is empty for {MessageId}", messageId);
                return new ClassificationResult
                {
                    Classification = "GeneralInquiry",
                    Confidence = 0.5m,
                    ExtractedData = null
                };
            }

            _logger.LogDebug("Email text length: {Length} characters", emailText.Length);

            // Truncate if too long (OpenAI has token limits)
            if (emailText.Length > 8000)
            {
                _logger.LogWarning("Email text too long ({Length} chars), truncating to 8000", emailText.Length);
                emailText = emailText.Substring(0, 8000) + "...";
            }

            // Build conversation context
            string? conversationContext = null;
            try
            {
                _logger.LogInformation("Building conversation context for conversation {ConversationId}", message.ConversationId);
                conversationContext = await BuildConversationContextAsync(message.ConversationId, message.Id, ct);
                if (!string.IsNullOrEmpty(conversationContext))
                {
                    _logger.LogInformation("Conversation context built successfully, length: {Length} characters", conversationContext.Length);
                }
                else
                {
                    _logger.LogInformation("No conversation context available, using single email");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build conversation context, proceeding with single email analysis");
            }

            // Classify using OpenAI with context
            _logger.LogInformation("Calling AI for classification (with context: {HasContext})", !string.IsNullOrEmpty(conversationContext));
            var classification = await ClassifyWithOpenAIAsync(emailText, conversationContext, ct);
            _logger.LogInformation("AI classification result: {Classification} (confidence: {Confidence:P0})", 
                classification.Classification, classification.Confidence);
            
            // Extract data with context
            _logger.LogInformation("Calling AI for data extraction (classification: {Classification})", classification.Classification);
            var extractedData = await ExtractDataWithOpenAIAsync(emailText, classification.Classification, conversationContext, ct);
            _logger.LogInformation("AI extraction completed. Extracted fields: {Fields}", 
                string.Join(", ", extractedData.Where(kv => kv.Value != null).Select(kv => $"{kv.Key}={kv.Value}")));

            // Create or update extracted data
            var existingData = await _context.EmailExtractedData
                .FirstOrDefaultAsync(ed => ed.ConversationId == message.ConversationId, ct);

            if (existingData != null)
            {
                UpdateExtractedData(existingData, classification, extractedData, message.Id);
                _context.EmailExtractedData.Update(existingData);
            }
            else
            {
                var newData = CreateExtractedData(message.ConversationId, message.Id, classification, extractedData);
                _context.EmailExtractedData.Add(newData);
                existingData = newData;
            }

            // Generate summaries for middle messages if needed (handled in BuildConversationContextAsync)
            // Save any summary updates
            await _context.SaveChangesAsync(ct);

            // Generate suggested response
            string suggestedResponse = string.Empty;
            try
            {
                _logger.LogInformation("Generating AI suggested response for classification: {Classification}", classification.Classification);
                var contextForResponse = string.IsNullOrEmpty(conversationContext) 
                    ? $"Email content:\n\n{emailText}" 
                    : conversationContext;
                suggestedResponse = await GenerateSuggestedResponseAsync(
                    contextForResponse, 
                    classification.Classification, 
                    existingData, 
                    ct);
                _logger.LogInformation("AI suggested response generated, length: {Length} characters", suggestedResponse.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate suggested response, continuing without it");
            }

            // Create AI analysis message
            try
            {
                _logger.LogInformation("Creating AI analysis message for conversation {ConversationId}", message.ConversationId);
                await CreateAIAnalysisMessageAsync(
                    message.ConversationId,
                    classification.Classification,
                    classification.Confidence,
                    existingData,
                    suggestedResponse,
                    ct);
                _logger.LogInformation("AI analysis message created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create AI analysis message, continuing");
            }

            _logger.LogInformation("AI classification completed successfully for message {MessageId}. Result: {Classification} ({Confidence:P0})", 
                messageId, classification.Classification, classification.Confidence);

            // Update conversation classification
            message.Conversation.Classification = classification.Classification;
            message.Conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return new ClassificationResult
            {
                Classification = classification.Classification,
                Confidence = classification.Confidence,
                ExtractedData = existingData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying email {MessageId}", messageId);
            throw;
        }
    }

    private async Task<(string Classification, decimal Confidence)> ClassifyWithOpenAIAsync(
        string emailText,
        string? conversationContext = null,
        CancellationToken ct = default)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        var systemPrompt = @"You are an email classifier for a restaurant booking system.
Classify this email into one of these categories:
- GeneralInquiry
- Sponsorship
- Marketing
- Accounting
- GroupBooking
- BuffetBooking
- TableBooking
- Complaint

Respond with ONLY a JSON object in this exact format:
{
  ""classification"": ""CategoryName"",
  ""confidence"": 0.0-1.0
}";

        var chatMessages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage($"Email content:\n\n{emailText}")
        };

        // Note: Some models (like gpt-4o-mini) don't support custom temperature, only default (1)
        var chatOptions = new ChatCompletionOptions();

        var response = await _openAIClient!.GetChatClient(model).CompleteChatAsync(
            chatMessages,
            chatOptions,
            ct);

        var content = response.Value.Content[0].Text;
        var result = ParseClassificationResponse(content);

        return (result.Classification, result.Confidence);
    }

    private async Task<Dictionary<string, object?>> ExtractDataWithOpenAIAsync(
        string emailText,
        string classification,
        string? conversationContext = null,
        CancellationToken ct = default)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _logger.LogInformation("Calling OpenAI API for data extraction. Model: {Model}, Classification: {Classification}, HasContext: {HasContext}", 
            model, classification, !string.IsNullOrEmpty(conversationContext));

        var systemPrompt = $@"You are extracting structured data from an email for a restaurant booking system in Iceland. 
The email has been classified as: {classification}

IMPORTANT: Emails are typically in Icelandic, but English emails also occur. Extract information regardless of language.

Extract the following fields carefully (use null if not found or unclear):

1. ContactName (string or null):
   - The name of the person making the request/booking
   - Look for names in signatures, greetings, or body text
   - Common Icelandic name patterns: FirstName LastName (e.g., ""Jón Jónsson"", ""Anna Sigurðardóttir"")
   - Common English patterns: ""John Smith"", ""Jane Doe""
   - Extract the full name if available, otherwise null

2. ContactPhone (string or null):
   - Phone number in any format (Icelandic: +354, 354, or local format)
   - English: +1, +44, etc.
   - Look for patterns like: +354 XXX XXXX, 354-XXX-XXXX, XXX-XXXX, (XXX) XXX-XXXX
   - Include country code if present, otherwise include as-is

3. RequestedDate (ISO date format YYYY-MM-DD or null):
   - Date for the booking/request
   - Icelandic format: dd.MM.yyyy (e.g., ""15.02.2025"", ""15. febrúar 2025"")
   - English format: MM/dd/yyyy, dd/MM/yyyy, ""February 15, 2025"", ""15 Feb 2025""
   - Relative dates: ""today"", ""tomorrow"", ""next week"" - convert to actual date if context allows
   - Convert to ISO format YYYY-MM-DD

4. RequestedTime (HH:mm format or null):
   - Time for the booking/request
   - Formats: ""19:00"", ""7 PM"", ""19.00"", ""klukkan 19"", ""at 7pm""
   - Convert to 24-hour format HH:mm (e.g., ""19:00"" for 7 PM)

5. SpecialRequests (string or null):
   - Any special requests, dietary restrictions, notes, or additional information
   - Examples: ""window seat"", ""vegetarian"", ""birthday celebration"", ""wheelchair accessible""
   - Icelandic: ""gluggapláss"", ""grænmetisæta"", ""fæðingardagshátíð"", ""hjólastólaaðgengi""
   - Include full text of special requests, notes, or comments

Be thorough and extract all available information. If information appears in multiple places, use the most specific or complete version.
Respond with ONLY a JSON object containing these fields.";

        var userMessage = string.IsNullOrEmpty(conversationContext)
            ? $"Email content:\n\n{emailText}"
            : $"Conversation context:\n\n{conversationContext}\n\n---\n\nCurrent email to extract data from:\n\n{emailText}";

        var chatMessages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        // Note: Some models (like gpt-4o-mini) don't support custom temperature, only default (1)
        var chatOptions = new ChatCompletionOptions();

        var startTime = DateTime.UtcNow;
        var response = await _openAIClient!.GetChatClient(model).CompleteChatAsync(
            chatMessages,
            chatOptions,
            ct);
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

        var content = response.Value.Content[0].Text;
        _logger.LogInformation("OpenAI extraction response received in {Duration}ms. Response length: {Length} chars", 
            duration, content.Length);
        _logger.LogDebug("OpenAI extraction raw response: {Response}", content);

        var result = ParseExtractionResponse(content);
        _logger.LogInformation("Parsed extraction result with {Count} fields", result.Count);

        return result;
    }

    private (string Classification, decimal Confidence) ParseClassificationResponse(string content)
    {
        try
        {
            // Try to extract JSON from response
            var jsonMatch = Regex.Match(content, @"\{[^}]+\}");
            if (jsonMatch.Success)
            {
                var json = jsonMatch.Value;
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var classification = root.GetProperty("classification").GetString() ?? "GeneralInquiry";
                var confidence = root.TryGetProperty("confidence", out var confProp)
                    ? (decimal)confProp.GetDouble()
                    : 0.5m;

                return (classification, confidence);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse classification response: {Content}", content);
        }

        // Fallback
        return ("GeneralInquiry", 0.5m);
    }

    private Dictionary<string, object?> ParseExtractionResponse(string content)
    {
        var result = new Dictionary<string, object?>();

        try
        {
            // Try to extract JSON from response
            var jsonMatch = Regex.Match(content, @"\{[^}]+\}");
            if (jsonMatch.Success)
            {
                var json = jsonMatch.Value;
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Null)
                        result[prop.Name] = null;
                    else if (prop.Value.ValueKind == JsonValueKind.String)
                        result[prop.Name] = prop.Value.GetString();
                    else if (prop.Value.ValueKind == JsonValueKind.Number)
                        result[prop.Name] = prop.Value.GetInt32();
                    else
                        result[prop.Name] = prop.Value.GetRawText();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse extraction response: {Content}", content);
        }

        return result;
    }

    private EmailExtractedData CreateExtractedData(
        Guid conversationId,
        Guid messageId,
        (string Classification, decimal Confidence) classification,
        Dictionary<string, object?> extracted)
    {
        var data = new EmailExtractedData
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            MessageId = messageId,
            Classification = classification.Classification,
            Confidence = classification.Confidence,
            ExtractedJson = JsonSerializer.Serialize(extracted),
            ExtractedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Map extracted fields
        if (extracted.TryGetValue("RequestedDate", out var date) && date != null)
        {
            if (DateTime.TryParse(date.ToString(), out var parsedDate))
            {
                // Ensure UTC - PostgreSQL requires DateTime with Kind=UTC
                if (parsedDate.Kind == DateTimeKind.Unspecified)
                    parsedDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                else if (parsedDate.Kind == DateTimeKind.Local)
                    parsedDate = parsedDate.ToUniversalTime();
                
                data.RequestedDate = parsedDate;
            }
        }

        if (extracted.TryGetValue("RequestedTime", out var time) && time != null)
        {
            if (TimeSpan.TryParse(time.ToString(), out var parsedTime))
                data.RequestedTime = parsedTime;
        }

        if (extracted.TryGetValue("GuestCount", out var guestCount) && guestCount != null)
            data.GuestCount = Convert.ToInt32(guestCount);

        if (extracted.TryGetValue("AdultCount", out var adultCount) && adultCount != null)
            data.AdultCount = Convert.ToInt32(adultCount);

        if (extracted.TryGetValue("ChildCount", out var childCount) && childCount != null)
            data.ChildCount = Convert.ToInt32(childCount);

        if (extracted.TryGetValue("LocationCode", out var location) && location != null)
            data.LocationCode = location.ToString();

        if (extracted.TryGetValue("SpecialRequests", out var requests) && requests != null)
            data.SpecialRequests = requests.ToString();

        if (extracted.TryGetValue("ContactPhone", out var phone) && phone != null)
            data.ContactPhone = phone.ToString();

        if (extracted.TryGetValue("ContactEmail", out var email) && email != null)
            data.ContactEmail = email.ToString();

        if (extracted.TryGetValue("ContactName", out var name) && name != null)
            data.ContactName = name.ToString();

        return data;
    }

    private void UpdateExtractedData(
        EmailExtractedData existing,
        (string Classification, decimal Confidence) classification,
        Dictionary<string, object?> extracted,
        Guid messageId)
    {
        existing.Classification = classification.Classification;
        existing.Confidence = classification.Confidence;
        existing.MessageId = messageId;
        existing.ExtractedJson = JsonSerializer.Serialize(extracted);
        existing.ExtractedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;

        // Update extracted fields (same mapping as CreateExtractedData)
        if (extracted.TryGetValue("RequestedDate", out var date) && date != null)
        {
            if (DateTime.TryParse(date.ToString(), out var parsedDate))
            {
                // Ensure UTC - PostgreSQL requires DateTime with Kind=UTC
                if (parsedDate.Kind == DateTimeKind.Unspecified)
                    parsedDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                else if (parsedDate.Kind == DateTimeKind.Local)
                    parsedDate = parsedDate.ToUniversalTime();
                
                existing.RequestedDate = parsedDate;
            }
        }

        if (extracted.TryGetValue("RequestedTime", out var time) && time != null)
        {
            if (TimeSpan.TryParse(time.ToString(), out var parsedTime))
                existing.RequestedTime = parsedTime;
        }

        if (extracted.TryGetValue("GuestCount", out var guestCount) && guestCount != null)
            existing.GuestCount = Convert.ToInt32(guestCount);

        if (extracted.TryGetValue("AdultCount", out var adultCount) && adultCount != null)
            existing.AdultCount = Convert.ToInt32(adultCount);

        if (extracted.TryGetValue("ChildCount", out var childCount) && childCount != null)
            existing.ChildCount = Convert.ToInt32(childCount);

        if (extracted.TryGetValue("LocationCode", out var location) && location != null)
            existing.LocationCode = location.ToString();

        if (extracted.TryGetValue("SpecialRequests", out var requests) && requests != null)
            existing.SpecialRequests = requests.ToString();

        if (extracted.TryGetValue("ContactPhone", out var phone) && phone != null)
            existing.ContactPhone = phone.ToString();

        if (extracted.TryGetValue("ContactEmail", out var email) && email != null)
            existing.ContactEmail = email.ToString();

        if (extracted.TryGetValue("ContactName", out var name) && name != null)
            existing.ContactName = name.ToString();
    }

    private async Task<string> BuildConversationContextAsync(Guid conversationId, Guid currentMessageId, CancellationToken ct)
    {
        try
        {
            var maxContextMessages = _configuration.GetValue<int>("Email:MaxContextMessages", 10);
            _logger.LogInformation("Building conversation context for conversation {ConversationId}, max messages: {MaxMessages}", 
                conversationId, maxContextMessages);

            // Get all messages in conversation, ordered by received date
            var messages = await _context.EmailMessages
                .Where(m => m.ConversationId == conversationId && !m.IsAIResponse)
                .OrderBy(m => m.ReceivedDateTime)
                .ToListAsync(ct);

            _logger.LogInformation("Found {Count} messages in conversation", messages.Count);

            if (messages.Count == 0)
            {
                _logger.LogInformation("No messages found in conversation, returning empty context");
                return string.Empty;
            }

            // Limit to max context messages (take most recent N)
            if (messages.Count > maxContextMessages)
            {
                _logger.LogInformation("Limiting context to {MaxMessages} most recent messages (from {Total} total)", 
                    maxContextMessages, messages.Count);
                messages = messages.Skip(messages.Count - maxContextMessages).ToList();
            }

            var contextParts = new List<string>();
            var firstMessage = messages.First();
            var lastMessage = messages.Last();

            // First message: full body
            if (messages.Count > 0)
            {
                _logger.LogDebug("Fetching full body for first message {MessageId}", firstMessage.Id);
                string firstText;
                
                // If message is from ai@system.local, use database body
                if (string.Equals(firstMessage.FromEmail, "ai@system.local", StringComparison.OrdinalIgnoreCase))
                {
                    firstText = firstMessage.MessageBody ?? string.Empty;
                }
                else
                {
                    var firstBody = await _graphService.GetMessageBodyAsync(firstMessage.GraphMessageId, ct);
                    firstText = firstBody?.Text ?? firstBody?.Html ?? string.Empty;
                }
                
                if (!string.IsNullOrWhiteSpace(firstText))
                {
                    if (firstText.Length > 4000)
                        firstText = firstText.Substring(0, 4000) + "...";
                    contextParts.Add($"Email 1 (Full) - From: {firstMessage.FromName} ({firstMessage.FromEmail}), Date: {firstMessage.ReceivedDateTime:yyyy-MM-dd HH:mm}:\n{firstText}");
                    _logger.LogDebug("Added first message to context, length: {Length}", firstText.Length);
                }
            }

            // Middle messages: summaries
            if (messages.Count > 2)
            {
                _logger.LogInformation("Processing {Count} middle messages for summarization", messages.Count - 2);
                for (int i = 1; i < messages.Count - 1; i++)
                {
                    var msg = messages[i];
                    string summary;

                    // Use stored summary if available
                    if (!string.IsNullOrWhiteSpace(msg.AISummary))
                    {
                        _logger.LogDebug("Using stored summary for message {MessageId}", msg.Id);
                        summary = msg.AISummary;
                    }
                    else
                    {
                        // Generate summary
                        try
                        {
                            _logger.LogInformation("Generating summary for message {MessageId}", msg.Id);
                            string msgText;
                            
                            // If message is from ai@system.local, use database body
                            if (string.Equals(msg.FromEmail, "ai@system.local", StringComparison.OrdinalIgnoreCase))
                            {
                                msgText = msg.MessageBody ?? string.Empty;
                            }
                            else
                            {
                                var msgBody = await _graphService.GetMessageBodyAsync(msg.GraphMessageId, ct);
                                msgText = msgBody?.Text ?? msgBody?.Html ?? string.Empty;
                            }
                            
                            if (!string.IsNullOrWhiteSpace(msgText))
                            {
                                summary = await GenerateEmailSummaryAsync(msgText, ct);
                                // Store summary
                                msg.AISummary = summary;
                                _context.EmailMessages.Update(msg);
                                _logger.LogInformation("Generated and stored summary for message {MessageId}, length: {Length}", msg.Id, summary.Length);
                            }
                            else
                            {
                                summary = msg.Subject; // Fallback to subject
                                _logger.LogDebug("Using subject as fallback summary for message {MessageId}", msg.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate summary for message {MessageId}, using subject", msg.Id);
                            summary = msg.Subject; // Fallback to subject
                        }
                    }

                    contextParts.Add($"Email {i + 1} (Summary) - From: {msg.FromName} ({msg.FromEmail}), Date: {msg.ReceivedDateTime:yyyy-MM-dd HH:mm}:\n{summary}");
                }
            }

            // Last message: full body (if different from first)
            if (messages.Count > 1 && lastMessage.Id != firstMessage.Id)
            {
                _logger.LogDebug("Fetching full body for last message {MessageId}", lastMessage.Id);
                string lastText;
                
                // If message is from ai@system.local, use database body
                if (string.Equals(lastMessage.FromEmail, "ai@system.local", StringComparison.OrdinalIgnoreCase))
                {
                    lastText = lastMessage.MessageBody ?? string.Empty;
                }
                else
                {
                    var lastBody = await _graphService.GetMessageBodyAsync(lastMessage.GraphMessageId, ct);
                    lastText = lastBody?.Text ?? lastBody?.Html ?? string.Empty;
                }
                
                if (!string.IsNullOrWhiteSpace(lastText))
                {
                    if (lastText.Length > 4000)
                        lastText = lastText.Substring(0, 4000) + "...";
                    contextParts.Add($"Email {messages.Count} (Full) - From: {lastMessage.FromName} ({lastMessage.FromEmail}), Date: {lastMessage.ReceivedDateTime:yyyy-MM-dd HH:mm}:\n{lastText}");
                    _logger.LogDebug("Added last message to context, length: {Length}", lastText.Length);
                }
            }

            var finalContext = string.Join("\n\n---\n\n", contextParts);
            _logger.LogInformation("Conversation context built successfully. Total parts: {Parts}, Total length: {Length}", 
                contextParts.Count, finalContext.Length);
            return finalContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building conversation context for conversation {ConversationId}", conversationId);
            return string.Empty; // Fallback to empty context
        }
    }

    private async Task<string> GenerateEmailSummaryAsync(string emailBody, CancellationToken ct)
    {
        if (_openAIClient == null)
        {
            _logger.LogWarning("OpenAI client not available for summary generation");
            return string.Empty;
        }

        try
        {
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            _logger.LogInformation("Generating email summary. Model: {Model}, Input length: {Length}", model, emailBody.Length);

            // Truncate if too long
            var emailText = emailBody;
            if (emailText.Length > 4000)
            {
                _logger.LogWarning("Email text too long for summary ({Length} chars), truncating to 4000", emailText.Length);
                emailText = emailText.Substring(0, 4000) + "...";
            }

            var systemPrompt = @"You are summarizing emails for a restaurant booking system. 
Generate a concise 1-2 sentence summary of this email in Icelandic. Focus on key information like dates, times, requests, or important details.";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Email content:\n\n{emailText}")
            };

            // Note: Some models (like gpt-4o-mini) don't support custom temperature, only default (1)
            var chatOptions = new ChatCompletionOptions();

            var startTime = DateTime.UtcNow;
            var response = await _openAIClient.GetChatClient(model).CompleteChatAsync(
                chatMessages,
                chatOptions,
                ct);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var summary = response.Value.Content[0].Text?.Trim() ?? string.Empty;
            _logger.LogInformation("Email summary generated in {Duration}ms. Summary length: {Length}", duration, summary.Length);
            _logger.LogDebug("Generated summary: {Summary}", summary);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate email summary");
            return string.Empty;
        }
    }

    private async Task<string> GenerateSuggestedResponseAsync(
        string conversationContext,
        string classification,
        EmailExtractedData extractedData,
        CancellationToken ct)
    {
        if (_openAIClient == null)
        {
            _logger.LogWarning("OpenAI client not available for response generation");
            return string.Empty;
        }

        try
        {
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            _logger.LogInformation("Generating AI suggested response. Model: {Model}, Classification: {Classification}, Context length: {Length}", 
                model, classification, conversationContext.Length);

            var extractedInfo = new List<string>();
            if (!string.IsNullOrEmpty(extractedData.ContactName))
                extractedInfo.Add($"Nafn: {extractedData.ContactName}");
            if (!string.IsNullOrEmpty(extractedData.ContactPhone))
                extractedInfo.Add($"Sími: {extractedData.ContactPhone}");
            if (extractedData.RequestedDate.HasValue)
                extractedInfo.Add($"Dagsetning: {extractedData.RequestedDate.Value:yyyy-MM-dd}");
            if (extractedData.RequestedTime.HasValue)
                extractedInfo.Add($"Tími: {extractedData.RequestedTime.Value:hh\\:mm}");
            if (!string.IsNullOrEmpty(extractedData.SpecialRequests))
                extractedInfo.Add($"Sérstakar beiðnir: {extractedData.SpecialRequests}");

            var extractedInfoText = extractedInfo.Count > 0
                ? string.Join("\n", extractedInfo)
                : "Engar upplýsingar útdregnar.";

            _logger.LogDebug("Extracted info for response: {Info}", extractedInfoText);

            var systemPrompt = $@"Þú ert aðstoðarmaður fyrir veitingastað. Skrifaðu viðeigandi svar í íslensku byggt á flokkun og útdregnum upplýsingum.

Flokkun: {classification}

Útdregnar upplýsingar:
{extractedInfoText}

Skrifaðu vingjarnlegt og faglegt svar sem er viðeigandi fyrir þessa tegund beiðni. Svarið ætti að vera stutt og á punkti (2-4 setningar).";

            var userMessage = $"Samtalssaga:\n\n{conversationContext}";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            // Note: Some models (like gpt-4o-mini) don't support custom temperature, only default (1)
            var chatOptions = new ChatCompletionOptions();

            var startTime = DateTime.UtcNow;
            var response = await _openAIClient.GetChatClient(model).CompleteChatAsync(
                chatMessages,
                chatOptions,
                ct);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var responseText = response.Value.Content[0].Text?.Trim() ?? string.Empty;
            _logger.LogInformation("AI suggested response generated in {Duration}ms. Response length: {Length}", duration, responseText.Length);
            _logger.LogDebug("Generated response: {Response}", responseText);
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate suggested response");
            return string.Empty;
        }
    }

    private async Task<EmailMessage> CreateAIAnalysisMessageAsync(
        Guid conversationId,
        string classification,
        decimal confidence,
        EmailExtractedData extractedData,
        string suggestedResponse,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating AI analysis message for conversation {ConversationId}", conversationId);

        var extractedInfo = new List<string>();
        if (!string.IsNullOrEmpty(extractedData.ContactName))
            extractedInfo.Add($"- Name: {extractedData.ContactName}");
        if (!string.IsNullOrEmpty(extractedData.ContactPhone))
            extractedInfo.Add($"- Phone: {extractedData.ContactPhone}");
        if (extractedData.RequestedDate.HasValue)
            extractedInfo.Add($"- Date: {extractedData.RequestedDate.Value:yyyy-MM-dd}");
        if (extractedData.RequestedTime.HasValue)
            extractedInfo.Add($"- Time: {extractedData.RequestedTime.Value:hh\\:mm}");
        if (!string.IsNullOrEmpty(extractedData.SpecialRequests))
            extractedInfo.Add($"- Special Request: {extractedData.SpecialRequests}");

        var extractedInfoText = extractedInfo.Count > 0
            ? string.Join("\n", extractedInfo)
            : "No information extracted.";

        var messageBody = $@"=== AI Analysis ===

Classification: {classification} (Confidence: {confidence:P0})

Extracted Information:
{extractedInfoText}

Suggested Response:
{suggestedResponse}";

        var aiMessage = new EmailMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            GraphMessageId = $"ai-{Guid.NewGuid()}", // Unique ID for AI messages
            GraphConversationId = string.Empty,
            Subject = "AI Analysis",
            FromName = "AI Assistant",
            FromEmail = "ai@system.local",
            ToEmail = string.Empty,
            ToName = string.Empty,
            ReceivedDateTime = DateTime.UtcNow,
            SentDateTime = DateTime.UtcNow,
            IsRead = false,
            IsOutgoing = false,
            IsSentFromSystem = false,
            HasAttachments = false,
            AttachmentCount = 0,
            IsAIResponse = true,
            MessageBody = messageBody,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailMessages.Add(aiMessage);
        _logger.LogInformation("AI analysis message created with ID {MessageId}, body length: {Length}", aiMessage.Id, messageBody.Length);
        return aiMessage;
    }

    public async Task<ClassificationResult> RegenerateAIAnalysisAsync(Guid messageId, CancellationToken ct = default)
    {
        _logger.LogInformation("Regenerating AI analysis for message {MessageId}", messageId);

        if (_openAIClient == null)
        {
            _logger.LogWarning("OpenAI client not available, cannot regenerate analysis");
            throw new InvalidOperationException("OpenAI client not available");
        }

        try
        {
            // Get message from database
            var message = await _context.EmailMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId, ct);

            if (message == null)
            {
                _logger.LogWarning("Message {MessageId} not found in database", messageId);
                throw new InvalidOperationException($"Message {messageId} not found");
            }

            _logger.LogInformation("Re-running classification flow for message {MessageId} (GraphMessageId: {GraphMessageId})", 
                messageId, message.GraphMessageId);

            // Re-run classification flow (this will create a new AI analysis message)
            var result = await ClassifyEmailAsync(message.GraphMessageId, ct);
            
            _logger.LogInformation("AI analysis regeneration completed for message {MessageId}. New classification: {Classification}", 
                messageId, result.Classification);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating AI analysis for message {MessageId}", messageId);
            throw;
        }
    }
}

