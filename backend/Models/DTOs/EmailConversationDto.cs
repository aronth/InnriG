namespace InnriGreifi.API.Models.DTOs;

public class EmailConversationDto
{
    public Guid Id { get; set; }
    public string GraphConversationId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public string? Classification { get; set; }
    public string Priority { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime LastMessageReceivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public EmailExtractedDataDto? ExtractedData { get; set; }
    public List<EmailMessageDto>? Messages { get; set; }
}

