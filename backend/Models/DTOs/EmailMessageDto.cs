namespace InnriGreifi.API.Models.DTOs;

public class EmailMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string GraphMessageId { get; set; } = string.Empty;
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
    public bool IsOutgoing { get; set; }
    public bool HasAttachments { get; set; }
    public int AttachmentCount { get; set; }
    public string? Importance { get; set; }
    public bool IsAIResponse { get; set; }
    public string? MessageBody { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ClassificationQueueStatus { get; set; } // Pending, Processing, Completed, Failed, null if not queued
    public DateTime? ClassificationQueuedAt { get; set; }
    public DateTime? ClassificationCompletedAt { get; set; }
}

