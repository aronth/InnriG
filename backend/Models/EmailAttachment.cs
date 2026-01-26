using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class EmailAttachment
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    [MaxLength(500)]
    public string GraphAttachmentId { get; set; } = string.Empty; // Graph's attachment ID

    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; } // Bytes

    public bool IsInline { get; set; }

    [MaxLength(200)]
    public string? ContentId { get; set; } // For inline attachments

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public EmailMessage Message { get; set; } = null!;
}

