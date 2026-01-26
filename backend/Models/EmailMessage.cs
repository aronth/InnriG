using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class EmailMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    [MaxLength(500)]
    public string GraphMessageId { get; set; } = string.Empty; // Graph's message ID (unique)

    [MaxLength(500)]
    public string GraphConversationId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? InReplyToId { get; set; } // Graph Message-ID of parent

    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(300)]
    public string FromEmail { get; set; } = string.Empty;

    [MaxLength(300)]
    public string FromName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string ToEmail { get; set; } = string.Empty;

    [MaxLength(300)]
    public string ToName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Cc { get; set; } // Comma-separated

    [MaxLength(1000)]
    public string? Bcc { get; set; } // Comma-separated

    public DateTime ReceivedDateTime { get; set; }

    public DateTime SentDateTime { get; set; }

    public bool IsRead { get; set; }

    public bool IsOutgoing { get; set; } // True if sent from our system

    public bool IsSentFromSystem { get; set; } // True if sent via our system

    public Guid? SentByUserId { get; set; } // Who sent it (if sent from system)

    public bool HasAttachments { get; set; }

    public int AttachmentCount { get; set; }

    [MaxLength(20)]
    public string? Importance { get; set; } // Low, Normal, High

    [MaxLength(50)]
    public string? Flag { get; set; } // Flag status if any

    public bool IsAIResponse { get; set; } // True if this is an AI-generated analysis message

    public string? MessageBody { get; set; } // Message body text (for AI messages)

    [MaxLength(500)]
    public string? AISummary { get; set; } // AI-generated summary of this email (for context building)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public EmailConversation Conversation { get; set; } = null!;

    [JsonIgnore]
    public User? SentBy { get; set; }

    [JsonIgnore]
    public List<EmailAttachment> Attachments { get; set; } = new();
}

