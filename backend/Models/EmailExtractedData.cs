using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class EmailExtractedData
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Guid? MessageId { get; set; } // Which message extracted from

    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string Classification { get; set; } = string.Empty;

    public decimal Confidence { get; set; } // 0-1

    public DateTime? RequestedDate { get; set; }

    public TimeSpan? RequestedTime { get; set; }

    public int? GuestCount { get; set; }

    public int? AdultCount { get; set; }

    public int? ChildCount { get; set; }

    [MaxLength(100)]
    public string? LocationCode { get; set; } // Restaurant/location

    [MaxLength(2000)]
    public string? SpecialRequests { get; set; } // Free text

    [MaxLength(100)]
    public string? ContactPhone { get; set; }

    [MaxLength(300)]
    public string? ContactEmail { get; set; }

    [MaxLength(300)]
    public string? ContactName { get; set; } // Extracted name from email

    public string? ExtractedJson { get; set; } // Full JSON of all extracted fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public EmailConversation Conversation { get; set; } = null!;

    [JsonIgnore]
    public EmailMessage? Message { get; set; }
}

