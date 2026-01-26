using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class EmailConversation
{
    public Guid Id { get; set; }

    [MaxLength(500)]
    public string GraphConversationId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? GraphThreadId { get; set; }

    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(300)]
    public string FromEmail { get; set; } = string.Empty;

    [MaxLength(300)]
    public string FromName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = "New"; // New, InProgress, AwaitingResponse, Resolved, Archived

    public Guid? AssignedToUserId { get; set; }

    [MaxLength(100)]
    public string? Classification { get; set; } // BuffetBooking, TableBooking, Complaint, etc.

    [MaxLength(20)]
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

    public int MessageCount { get; set; }

    [MaxLength(500)]
    public string? LastMessageId { get; set; } // Graph Message-ID

    public DateTime LastMessageReceivedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public User? AssignedTo { get; set; }

    [JsonIgnore]
    public List<EmailMessage> Messages { get; set; } = new();

    [JsonIgnore]
    public EmailExtractedData? ExtractedData { get; set; }

    [JsonIgnore]
    public WorkflowInstance? WorkflowInstance { get; set; }
}

