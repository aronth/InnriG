using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Authentication.Azure;

namespace InnriGreifi.API.Services;

public class GraphEmailService : IGraphEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GraphEmailService> _logger;
    private GraphServiceClient? _graphClient;
    private readonly string? _sharedInboxEmail;
    private readonly bool _isConfigured;

    public GraphEmailService(IConfiguration configuration, ILogger<GraphEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _sharedInboxEmail = configuration["Email:SharedInboxEmail"];
        _isConfigured = EmailConfigurationHelper.IsEmailConfigured(configuration);
        
        if (!_isConfigured)
        {
            _logger.LogWarning("Email configuration is incomplete. Email functionality will be disabled.");
        }
    }

    private GraphServiceClient? GetGraphClient()
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("Email is not configured. Cannot create Graph client.");
            return null;
        }

        if (_graphClient != null)
            return _graphClient;

        var tenantId = _configuration["Email:TenantId"];
        var clientId = _configuration["Email:ClientId"];
        var clientSecret = _configuration["Email:ClientSecret"];

        if (string.IsNullOrWhiteSpace(tenantId) || 
            string.IsNullOrWhiteSpace(clientId) || 
            string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("Email configuration is incomplete. Cannot create Graph client.");
            return null;
        }

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var authProvider = new AzureIdentityAuthenticationProvider(credential, scopes: new[] { "https://graph.microsoft.com/.default" });
        
        _graphClient = new GraphServiceClient(authProvider);
        return _graphClient;
    }

    public async Task<List<GraphMessageInfo>> GetMessagesAsync(DateTime? since = null, CancellationToken ct = default)
    {
        try
        {
            var client = GetGraphClient();
            if (client == null || string.IsNullOrWhiteSpace(_sharedInboxEmail))
            {
                _logger.LogWarning("Email is not configured. Cannot retrieve messages.");
                return new List<GraphMessageInfo>();
            }

            var messages = new List<GraphMessageInfo>();

            // Build filter for messages since a specific date
            var filter = since.HasValue 
                ? $"receivedDateTime ge {since.Value:yyyy-MM-ddTHH:mm:ssZ}"
                : null;

            var response = await client.Users[_sharedInboxEmail].Messages.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters = new Microsoft.Graph.Users.Item.Messages.MessagesRequestBuilder.MessagesRequestBuilderGetQueryParameters
                {
                    Filter = filter,
                    Orderby = new[] { "receivedDateTime desc" },
                    Top = 100,
                    Select = new[] { 
                        "id", "conversationId", "subject", "from", "toRecipients", 
                        "ccRecipients", "bccRecipients", "receivedDateTime", "sentDateTime", 
                        "isRead", "hasAttachments", "importance", "flag", "internetMessageId"
                    }
                };
            }, ct);

            if (response?.Value == null)
                return messages;

            foreach (var msg in response.Value)
            {
                var messageInfo = MapToGraphMessageInfo(msg);
                if (messageInfo != null)
                    messages.Add(messageInfo);
            }

            _logger.LogInformation("Retrieved {Count} messages from shared inbox", messages.Count);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages from Graph API");
            throw;
        }
    }

    public async Task<GraphMessageDetail?> GetMessageAsync(string messageId, CancellationToken ct = default)
    {
        try
        {
            var client = GetGraphClient();
            if (client == null || string.IsNullOrWhiteSpace(_sharedInboxEmail))
            {
                _logger.LogWarning("Email is not configured. Cannot retrieve message {MessageId}.", messageId);
                return null;
            }
            
            var message = await client.Users[_sharedInboxEmail].Messages[messageId].GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters = new Microsoft.Graph.Users.Item.Messages.Item.MessageItemRequestBuilder.MessageItemRequestBuilderGetQueryParameters
                {
                    Select = new[] { 
                        "id", "conversationId", "subject", "from", "toRecipients", 
                        "ccRecipients", "bccRecipients", "receivedDateTime", "sentDateTime", 
                        "isRead", "hasAttachments", "importance", "flag", "internetMessageId",
                        "body", "bodyPreview"
                    }
                };
            }, ct);

            if (message == null)
                return null;

            var detail = MapToGraphMessageDetail(message);
            return detail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message {MessageId} from Graph API", messageId);
            throw;
        }
    }

    public async Task<GraphMessageBody?> GetMessageBodyAsync(string messageId, CancellationToken ct = default)
    {
        try
        {
            var client = GetGraphClient();
            if (client == null || string.IsNullOrWhiteSpace(_sharedInboxEmail))
            {
                _logger.LogWarning("Email is not configured. Cannot retrieve message body {MessageId}.", messageId);
                return null;
            }
            
            var message = await client.Users[_sharedInboxEmail].Messages[messageId].GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters = new Microsoft.Graph.Users.Item.Messages.Item.MessageItemRequestBuilder.MessageItemRequestBuilderGetQueryParameters
                {
                    Select = new[] { "body", "bodyPreview" }
                };
            }, ct);

            if (message?.Body == null)
                return null;

            return new GraphMessageBody
            {
                Html = message.Body.Content,
                Text = message.BodyPreview,
                ContentType = message.Body.ContentType?.ToString() ?? "Text"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message body {MessageId} from Graph API", messageId);
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(string messageId, CancellationToken ct = default)
    {
        try
        {
            var client = GetGraphClient();
            if (client == null || string.IsNullOrWhiteSpace(_sharedInboxEmail))
            {
                _logger.LogWarning("Email is not configured. Cannot mark message {MessageId} as read.", messageId);
                return false;
            }
            
            var updateRequest = new Message
            {
                IsRead = true
            };

            await client.Users[_sharedInboxEmail].Messages[messageId].PatchAsync(updateRequest, cancellationToken: ct);
            
            _logger.LogInformation("Marked message {MessageId} as read", messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
            return false;
        }
    }

    private GraphMessageInfo? MapToGraphMessageInfo(Message msg)
    {
        if (msg.Id == null || msg.ConversationId == null)
            return null;

        return new GraphMessageInfo
        {
            Id = msg.Id,
            ConversationId = msg.ConversationId,
            ThreadId = null, // ThreadId not available in Graph SDK v5 Message model
            Subject = msg.Subject ?? string.Empty,
            FromEmail = msg.From?.EmailAddress?.Address ?? string.Empty,
            FromName = msg.From?.EmailAddress?.Name ?? string.Empty,
            ToEmail = msg.ToRecipients?.FirstOrDefault()?.EmailAddress?.Address ?? string.Empty,
            ToName = msg.ToRecipients?.FirstOrDefault()?.EmailAddress?.Name ?? string.Empty,
            Cc = msg.CcRecipients != null ? string.Join(", ", msg.CcRecipients.Select(r => r.EmailAddress?.Address).Where(a => !string.IsNullOrEmpty(a))) : null,
            Bcc = msg.BccRecipients != null ? string.Join(", ", msg.BccRecipients.Select(r => r.EmailAddress?.Address).Where(a => !string.IsNullOrEmpty(a))) : null,
            ReceivedDateTime = (msg.ReceivedDateTime ?? DateTimeOffset.UtcNow).UtcDateTime,
            SentDateTime = (msg.SentDateTime ?? DateTimeOffset.UtcNow).UtcDateTime,
            IsRead = msg.IsRead ?? false,
            HasAttachments = msg.HasAttachments ?? false,
            AttachmentCount = msg.Attachments?.Count ?? 0,
            Importance = msg.Importance?.ToString(),
            Flag = msg.Flag?.FlagStatus?.ToString(),
            InReplyTo = null // InReplyTo not directly available, would need to check headers
        };
    }

    private GraphMessageDetail MapToGraphMessageDetail(Message msg)
    {
        var info = MapToGraphMessageInfo(msg);
        if (info == null)
            throw new InvalidOperationException("Cannot map message to GraphMessageInfo");

        return new GraphMessageDetail
        {
            Id = info.Id,
            ConversationId = info.ConversationId,
            ThreadId = info.ThreadId,
            Subject = info.Subject,
            FromEmail = info.FromEmail,
            FromName = info.FromName,
            ToEmail = info.ToEmail,
            ToName = info.ToName,
            Cc = info.Cc,
            Bcc = info.Bcc,
            ReceivedDateTime = info.ReceivedDateTime,
            SentDateTime = info.SentDateTime,
            IsRead = info.IsRead,
            HasAttachments = info.HasAttachments,
            AttachmentCount = info.AttachmentCount,
            Importance = info.Importance,
            Flag = info.Flag,
            InReplyTo = info.InReplyTo,
            BodyHtml = msg.Body?.Content,
            BodyText = msg.BodyPreview,
            BodyContentType = msg.Body?.ContentType?.ToString() ?? "Text"
        };
    }

    public async Task<byte[]?> GetAttachmentContentAsync(string messageId, string attachmentId, CancellationToken ct = default)
    {
        try
        {
            var client = GetGraphClient();
            if (client == null || string.IsNullOrWhiteSpace(_sharedInboxEmail))
            {
                _logger.LogWarning("Email is not configured. Cannot retrieve attachment {AttachmentId} for message {MessageId}.", attachmentId, messageId);
                return null;
            }
            
            // Get the attachment object first
            var attachment = await client.Users[_sharedInboxEmail]
                .Messages[messageId]
                .Attachments[attachmentId]
                .GetAsync(requestConfiguration => { }, ct);

            // Check if it's a FileAttachment and get the content bytes
            if (attachment is Microsoft.Graph.Models.FileAttachment fileAttachment)
            {
                return fileAttachment.ContentBytes;
            }
            else if (attachment is Microsoft.Graph.Models.ItemAttachment itemAttachment)
            {
                // Handle item attachments if necessary, e.g., by returning null or throwing an exception
                _logger.LogWarning("Item attachment {AttachmentId} for message {MessageId} cannot be directly downloaded.", attachmentId, messageId);
                return null;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachment {AttachmentId} for message {MessageId} from Graph API", attachmentId, messageId);
            throw;
        }
    }

    public async Task<string?> SendReplyAsync(
        string fromEmail,
        string fromName,
        string toEmail,
        string toName,
        string subject,
        string bodyHtml,
        string? bodyText,
        string? inReplyToMessageId,
        string? conversationId,
        string? cc = null,
        string? bcc = null,
        CancellationToken ct = default)
    {
        try
        {
            var client = GetGraphClient();
            if (client == null)
            {
                _logger.LogWarning("Email is not configured. Cannot send reply email from {FromEmail} to {ToEmail}.", fromEmail, toEmail);
                throw new InvalidOperationException("Email is not configured. Cannot send emails.");
            }

            // Build recipients list
            var toRecipients = new List<Microsoft.Graph.Models.Recipient>
            {
                new Microsoft.Graph.Models.Recipient
                {
                    EmailAddress = new Microsoft.Graph.Models.EmailAddress
                    {
                        Address = toEmail,
                        Name = toName
                    }
                }
            };

            var ccRecipients = new List<Microsoft.Graph.Models.Recipient>();
            if (!string.IsNullOrEmpty(cc))
            {
                foreach (var ccEmail in cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    ccRecipients.Add(new Microsoft.Graph.Models.Recipient
                    {
                        EmailAddress = new Microsoft.Graph.Models.EmailAddress
                        {
                            Address = ccEmail
                        }
                    });
                }
            }

            var bccRecipients = new List<Microsoft.Graph.Models.Recipient>();
            if (!string.IsNullOrEmpty(bcc))
            {
                foreach (var bccEmail in bcc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    bccRecipients.Add(new Microsoft.Graph.Models.Recipient
                    {
                        EmailAddress = new Microsoft.Graph.Models.EmailAddress
                        {
                            Address = bccEmail
                        }
                    });
                }
            }

            // Create the message
            var message = new Microsoft.Graph.Models.Message
            {
                Subject = subject,
                Body = new Microsoft.Graph.Models.ItemBody
                {
                    ContentType = Microsoft.Graph.Models.BodyType.Html,
                    Content = bodyHtml
                },
                ToRecipients = toRecipients,
                CcRecipients = ccRecipients.Count > 0 ? ccRecipients : new List<Microsoft.Graph.Models.Recipient>(),
                BccRecipients = bccRecipients.Count > 0 ? bccRecipients : new List<Microsoft.Graph.Models.Recipient>()
            };

            // Note: Microsoft Graph API handles email threading automatically based on:
            // 1. The subject line (if it starts with "Re:" or "SV:")
            // 2. The conversation ID (if we were using the Reply endpoint)
            // Since we're using SendMail, threading will be handled by the subject line
            // Custom InternetMessageHeaders for In-Reply-To and References are not allowed
            // (they must start with 'x-' or 'X-')

            // Send the message from the specified user's mailbox
            // Use SendMail endpoint which properly handles sending
            var sendMailBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = true
            };

            await client.Users[fromEmail]
                .SendMail
                .PostAsync(sendMailBody, requestConfiguration => { }, ct);

            // Note: SendMail doesn't return the sent message ID directly
            // We'll need to query for it or use a different approach
            // For now, we'll return a placeholder - in production you might want to
            // query the sent items folder to get the actual message ID
            var sentMessageId = Guid.NewGuid().ToString();

            _logger.LogInformation("Sent reply email from {FromEmail} to {ToEmail} with subject {Subject}", 
                fromEmail, toEmail, subject);

            // Return a placeholder ID - in production, you might want to query sent items
            // to get the actual message ID after sending
            return sentMessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reply email from {FromEmail} to {ToEmail}", fromEmail, toEmail);
            throw;
        }
    }
}
