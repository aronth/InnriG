namespace InnriGreifi.API.Services;

public interface IGraphEmailService
{
    /// <summary>
    /// Gets new messages from the shared inbox since the specified date/time.
    /// </summary>
    Task<List<GraphMessageInfo>> GetMessagesAsync(DateTime? since = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a full message with body content from Graph API.
    /// </summary>
    Task<GraphMessageDetail?> GetMessageAsync(string messageId, CancellationToken ct = default);

    /// <summary>
    /// Gets the message body (HTML or text) from Graph API.
    /// </summary>
    Task<GraphMessageBody?> GetMessageBodyAsync(string messageId, CancellationToken ct = default);

    /// <summary>
    /// Marks a message as read in Graph API.
    /// </summary>
    Task<bool> MarkAsReadAsync(string messageId, CancellationToken ct = default);

    /// <summary>
    /// Gets attachment content from Graph API.
    /// </summary>
    Task<byte[]?> GetAttachmentContentAsync(string messageId, string attachmentId, CancellationToken ct = default);

    /// <summary>
    /// Sends a reply email via Graph API.
    /// </summary>
    Task<string?> SendReplyAsync(
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
        CancellationToken ct = default);

    /// <summary>
    /// Sends a reply email via Graph API using delegated authentication.
    /// </summary>
    Task<string?> SendReplyAsync(
        Guid? userId,
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
        CancellationToken ct = default);
}

/// <summary>
/// Lightweight message info for listing (metadata only).
/// </summary>
public class GraphMessageInfo
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string? ThreadId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public DateTime ReceivedDateTime { get; set; }
    public DateTime SentDateTime { get; set; }
    public bool IsRead { get; set; }
    public bool HasAttachments { get; set; }
    public int AttachmentCount { get; set; }
    public string? Importance { get; set; }
    public string? Flag { get; set; }
    public string? InReplyTo { get; set; }
}

/// <summary>
/// Full message detail with body content.
/// </summary>
public class GraphMessageDetail : GraphMessageInfo
{
    public string? BodyHtml { get; set; }
    public string? BodyText { get; set; }
    public string? BodyContentType { get; set; }
}

/// <summary>
/// Message body content.
/// </summary>
public class GraphMessageBody
{
    public string? Html { get; set; }
    public string? Text { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

