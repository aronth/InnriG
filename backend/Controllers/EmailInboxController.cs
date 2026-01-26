using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/emails")]
[Authorize(Roles = "User,Manager,Admin")]
public class EmailInboxController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGraphEmailService _graphService;
    private readonly IEmailClassificationQueueService _queueService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<EmailInboxController> _logger;

    public EmailInboxController(
        AppDbContext context,
        IGraphEmailService graphService,
        IEmailClassificationQueueService queueService,
        UserManager<User> userManager,
        ILogger<EmailInboxController> logger)
    {
        _context = context;
        _graphService = graphService;
        _queueService = queueService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] string? status = null,
        [FromQuery] Guid? assignedTo = null,
        [FromQuery] string? classification = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.EmailConversations
                .Include(c => c.AssignedTo)
                .Include(c => c.ExtractedData)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);

            if (assignedTo.HasValue)
                query = query.Where(c => c.AssignedToUserId == assignedTo.Value);

            if (!string.IsNullOrEmpty(classification))
                query = query.Where(c => c.Classification == classification);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var conversations = await query
                .OrderByDescending(c => c.LastMessageReceivedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = conversations.Select(c => MapToConversationDto(c)).ToList();

            return Ok(new
            {
                items = dtos,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return StatusCode(500, new { error = "Error getting conversations", details = ex.Message });
        }
    }

    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        try
        {
            var conversation = await _context.EmailConversations
                .Include(c => c.AssignedTo)
                .Include(c => c.ExtractedData)
                .Include(c => c.Messages.OrderBy(m => m.ReceivedDateTime))
                    .ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation == null)
                return NotFound();

            // Get queue status for all messages
            var messageIds = conversation.Messages.Select(m => m.Id).ToList();
            var queueItems = await _context.EmailClassificationQueues
                .Where(q => messageIds.Contains(q.MessageId))
                .ToListAsync();

            var queueDict = queueItems.ToDictionary(q => q.MessageId);

            var dto = MapToConversationDto(conversation);
            dto.Messages = conversation.Messages.Select(m => MapToMessageDto(m, queueDict.GetValueOrDefault(m.Id))).ToList();

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {Id}", id);
            return StatusCode(500, new { error = "Error getting conversation", details = ex.Message });
        }
    }

    [HttpGet("messages/{id}/body")]
    public async Task<IActionResult> GetMessageBody(Guid id)
    {
        try
        {
            var message = await _context.EmailMessages
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
                return NotFound();

            // Get inline attachments for this message
            var inlineAttachments = await _context.EmailAttachments
                .Where(a => a.MessageId == id && a.IsInline && !string.IsNullOrEmpty(a.ContentId))
                .ToListAsync();

            // If message is from ai@system.local or was sent from our system, fetch body from database
            if (string.Equals(message.FromEmail, "ai@system.local", StringComparison.OrdinalIgnoreCase) || 
                message.IsSentFromSystem)
            {
                if (string.IsNullOrEmpty(message.MessageBody))
                    return NotFound();

                // Determine content type based on whether the body contains HTML tags
                var contentType = message.MessageBody.Contains("<") && message.MessageBody.Contains(">")
                    ? "Html"
                    : "Text";

                var html = contentType == "Html" ? message.MessageBody : null;
                
                // Replace cid: URLs with attachment URLs
                if (html != null && inlineAttachments.Any())
                {
                    html = ReplaceCidUrls(html, inlineAttachments, id);
                }

                var dto = new EmailMessageBodyDto
                {
                    Html = html,
                    Text = contentType == "Text" ? message.MessageBody : null,
                    ContentType = contentType
                };

                return Ok(dto);
            }

            // For messages sent from our system, we might have stored the body even if IsSentFromSystem isn't set
            // Check if GraphMessageId looks like a GUID (our placeholder) and we have the body in database
            if (!string.IsNullOrEmpty(message.MessageBody) && 
                Guid.TryParse(message.GraphMessageId, out _) && 
                message.GraphMessageId.Length == 36)
            {
                // This is likely a placeholder GUID, use database body
                var contentType = message.MessageBody.Contains("<") && message.MessageBody.Contains(">")
                    ? "Html"
                    : "Text";

                var html = contentType == "Html" ? message.MessageBody : null;
                
                if (html != null && inlineAttachments.Any())
                {
                    html = ReplaceCidUrls(html, inlineAttachments, id);
                }

                return Ok(new EmailMessageBodyDto
                {
                    Html = html,
                    Text = contentType == "Text" ? message.MessageBody : null,
                    ContentType = contentType
                });
            }

            // Fetch body from Graph API for other messages (only if we have a valid Graph message ID)
            if (string.IsNullOrEmpty(message.GraphMessageId))
                return NotFound();

            // Check if GraphMessageId is a GUID (placeholder) - Graph API message IDs are base64-encoded strings, not GUIDs
            if (Guid.TryParse(message.GraphMessageId, out _) && message.GraphMessageId.Length == 36)
            {
                // This is a placeholder GUID, not a real Graph API message ID
                // If we don't have the body in database, we can't fetch it
                _logger.LogWarning("Attempted to fetch message body for message {MessageId} with placeholder GUID {GraphMessageId}, but no body stored in database", 
                    id, message.GraphMessageId);
                return NotFound();
            }

            var body = await _graphService.GetMessageBodyAsync(message.GraphMessageId);

            if (body == null)
                return NotFound();

            var graphHtml = body.Html;
            
            // Replace cid: URLs with attachment URLs
            if (graphHtml != null && inlineAttachments.Any())
            {
                graphHtml = ReplaceCidUrls(graphHtml, inlineAttachments, id);
            }

            var graphDto = new EmailMessageBodyDto
            {
                Html = graphHtml,
                Text = body.Text,
                ContentType = body.ContentType
            };

            return Ok(graphDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message body {Id}", id);
            return StatusCode(500, new { error = "Error getting message body", details = ex.Message });
        }
    }

    [HttpGet("messages/{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> GetAttachment(Guid id, Guid attachmentId)
    {
        try
        {
            var attachment = await _context.EmailAttachments
                .Include(a => a.Message)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.MessageId == id);

            if (attachment == null)
                return NotFound();

            // Check if GraphMessageId is a GUID (placeholder) - Graph API message IDs are base64-encoded strings, not GUIDs
            if (Guid.TryParse(attachment.Message.GraphMessageId, out _) && attachment.Message.GraphMessageId.Length == 36)
            {
                // This is a placeholder GUID, not a real Graph API message ID
                // Attachments for messages sent from our system should be stored differently or not available
                _logger.LogWarning("Attempted to fetch attachment {AttachmentId} for message {MessageId} with placeholder GUID {GraphMessageId}", 
                    attachmentId, id, attachment.Message.GraphMessageId);
                return NotFound();
            }

            // Fetch attachment content from Graph API
            var content = await _graphService.GetAttachmentContentAsync(
                attachment.Message.GraphMessageId, 
                attachment.GraphAttachmentId);

            if (content == null)
                return NotFound();

            return File(content, attachment.ContentType, attachment.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachment {AttachmentId} for message {MessageId}", attachmentId, id);
            return StatusCode(500, new { error = "Error getting attachment", details = ex.Message });
        }
    }

    private string ReplaceCidUrls(string html, List<EmailAttachment> attachments, Guid messageId)
    {
        // Build base URL for attachments
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var attachmentBaseUrl = $"{baseUrl}/api/emails/messages/{messageId}/attachments";
        
        foreach (var attachment in attachments)
        {
            if (string.IsNullOrEmpty(attachment.ContentId))
                continue;

            var attachmentUrl = $"{attachmentBaseUrl}/{attachment.Id}";
            
            // Extract just the filename part from ContentId (e.g., "image001.jpg@01DC8BAB.E97E0190" -> "image001.jpg")
            var contentIdParts = attachment.ContentId.Split('@');
            var contentIdBase = contentIdParts[0];
            var fullContentId = attachment.ContentId;

            // Create patterns to match - cid URLs can be in various formats
            var patterns = new List<string>
            {
                fullContentId,
                contentIdBase,
                fullContentId.ToLower(),
                contentIdBase.ToLower(),
                fullContentId.ToUpper(),
                contentIdBase.ToUpper(),
                System.Net.WebUtility.UrlEncode(fullContentId),
                System.Net.WebUtility.UrlEncode(contentIdBase)
            };

            foreach (var pattern in patterns)
            {
                // Replace cid: URLs in src attributes (case-insensitive)
                // Match: src="cid:..." or src='cid:...'
                html = System.Text.RegularExpressions.Regex.Replace(
                    html,
                    $@"src=[""']cid:{System.Text.RegularExpressions.Regex.Escape(pattern)}[""']",
                    $"src=\"{attachmentUrl}\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Replace cid: URLs in href attributes
                html = System.Text.RegularExpressions.Regex.Replace(
                    html,
                    $@"href=[""']cid:{System.Text.RegularExpressions.Regex.Escape(pattern)}[""']",
                    $"href=\"{attachmentUrl}\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }

        return html;
    }

    [HttpPut("conversations/{id}/assign")]
    public async Task<IActionResult> AssignConversation(Guid id, [FromBody] AssignConversationDto dto)
    {
        try
        {
            var conversation = await _context.EmailConversations.FindAsync(id);
            if (conversation == null)
                return NotFound();

            conversation.AssignedToUserId = dto.UserId;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Conversation assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning conversation {Id}", id);
            return StatusCode(500, new { error = "Error assigning conversation", details = ex.Message });
        }
    }

    [HttpPut("conversations/{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var conversation = await _context.EmailConversations.FindAsync(id);
            if (conversation == null)
                return NotFound();

            conversation.Status = dto.Status;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for conversation {Id}", id);
            return StatusCode(500, new { error = "Error updating status", details = ex.Message });
        }
    }

    [HttpPost("conversations/{id}/reparse")]
    public async Task<IActionResult> ReparseConversation(Guid id)
    {
        try
        {
            var conversation = await _context.EmailConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation == null)
                return NotFound();

            // Get the first message in the conversation (or most recent)
            var message = conversation.Messages
                .OrderByDescending(m => m.ReceivedDateTime)
                .FirstOrDefault();

            if (message == null)
                return BadRequest(new { error = "No messages found in conversation" });

            // Enqueue the message for re-classification
            await _queueService.EnqueueClassificationAsync(message.Id);

            _logger.LogInformation("Re-queued conversation {ConversationId} for classification (message {MessageId})", 
                id, message.Id);

            return Ok(new { message = "Conversation queued for re-classification" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-parsing conversation {Id}", id);
            return StatusCode(500, new { error = "Error re-parsing conversation", details = ex.Message });
        }
    }

    private EmailConversationDto MapToConversationDto(EmailConversation c)
    {
        return new EmailConversationDto
        {
            Id = c.Id,
            GraphConversationId = c.GraphConversationId,
            Subject = c.Subject,
            FromEmail = c.FromEmail,
            FromName = c.FromName,
            Status = c.Status,
            AssignedToUserId = c.AssignedToUserId,
            AssignedToName = c.AssignedTo?.Name,
            Classification = c.Classification,
            Priority = c.Priority,
            MessageCount = c.MessageCount,
            LastMessageReceivedAt = c.LastMessageReceivedAt,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            ExtractedData = c.ExtractedData != null ? MapToExtractedDataDto(c.ExtractedData) : null
        };
    }

    private EmailMessageDto MapToMessageDto(EmailMessage m, EmailClassificationQueue? queueItem = null)
    {
        return new EmailMessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            GraphMessageId = m.GraphMessageId,
            Subject = m.Subject,
            FromEmail = m.FromEmail,
            FromName = m.FromName,
            ToEmail = m.ToEmail,
            ToName = m.ToName,
            Cc = m.Cc,
            Bcc = m.Bcc,
            ReceivedDateTime = m.ReceivedDateTime,
            SentDateTime = m.SentDateTime,
            IsRead = m.IsRead,
            IsOutgoing = m.IsOutgoing,
            HasAttachments = m.HasAttachments,
            AttachmentCount = m.AttachmentCount,
            Importance = m.Importance,
            IsAIResponse = m.IsAIResponse,
            MessageBody = m.MessageBody,
            CreatedAt = m.CreatedAt,
            ClassificationQueueStatus = queueItem?.Status,
            ClassificationQueuedAt = queueItem?.QueuedAt,
            ClassificationCompletedAt = queueItem?.CompletedAt
        };
    }

    private EmailExtractedDataDto MapToExtractedDataDto(EmailExtractedData ed)
    {
        return new EmailExtractedDataDto
        {
            Id = ed.Id,
            Classification = ed.Classification,
            Confidence = ed.Confidence,
            RequestedDate = ed.RequestedDate,
            RequestedTime = ed.RequestedTime,
            GuestCount = ed.GuestCount,
            AdultCount = ed.AdultCount,
            ChildCount = ed.ChildCount,
            LocationCode = ed.LocationCode,
            SpecialRequests = ed.SpecialRequests,
            ContactPhone = ed.ContactPhone,
            ContactEmail = ed.ContactEmail,
            ContactName = ed.ContactName,
            ExtractedAt = ed.ExtractedAt
        };
    }

    [HttpPost("messages/{id}/regenerate-ai")]
    public async Task<IActionResult> RegenerateAIAnalysis(Guid id)
    {
        try
        {
            var classificationService = HttpContext.RequestServices.GetRequiredService<IEmailClassificationService>();
            var result = await classificationService.RegenerateAIAnalysisAsync(id);

            // Reload conversation to return updated data
            var message = await _context.EmailMessages
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
                return NotFound();

            var conversation = await _context.EmailConversations
                .Include(c => c.AssignedTo)
                .Include(c => c.ExtractedData)
                .Include(c => c.Messages.OrderBy(m => m.ReceivedDateTime))
                .FirstOrDefaultAsync(c => c.Id == message.ConversationId);

            if (conversation == null)
                return NotFound();

            // Get queue status for all messages
            var messageIds = conversation.Messages.Select(m => m.Id).ToList();
            var queueItems = await _context.EmailClassificationQueues
                .Where(q => messageIds.Contains(q.MessageId))
                .ToListAsync();

            var queueDict = queueItems.ToDictionary(q => q.MessageId);

            var dto = MapToConversationDto(conversation);
            dto.Messages = conversation.Messages.Select(m => MapToMessageDto(m, queueDict.GetValueOrDefault(m.Id))).ToList();

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating AI analysis for message {Id}", id);
            return StatusCode(500, new { error = "Error regenerating AI analysis", details = ex.Message });
        }
    }

    [HttpPost("conversations/{id}/reply")]
    public async Task<IActionResult> ReplyToConversation(Guid id, [FromBody] ReplyToConversationDto dto)
    {
        try
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            // Get the conversation
            var conversation = await _context.EmailConversations
                .Include(c => c.Messages.OrderByDescending(m => m.ReceivedDateTime))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation == null)
                return NotFound();

            // Get the user's email mapping (use specified email or default)
            UserEmailMapping? emailMapping = null;
            if (!string.IsNullOrEmpty(dto.FromEmailAddress))
            {
                emailMapping = await _context.UserEmailMappings
                    .FirstOrDefaultAsync(m => m.UserId == currentUser.Id && m.EmailAddress == dto.FromEmailAddress);
            }
            else
            {
                emailMapping = await _context.UserEmailMappings
                    .FirstOrDefaultAsync(m => m.UserId == currentUser.Id && m.IsDefault);
            }

            if (emailMapping == null)
                return BadRequest(new { error = "No email address configured. Please link an email address to your account." });

            // Get the original customer message (exclude AI messages - they're just context)
            // Find the first non-AI message from the customer
            var originalMessage = conversation.Messages
                .Where(m => !m.IsAIResponse && !m.IsOutgoing)
                .OrderByDescending(m => m.ReceivedDateTime)
                .FirstOrDefault();
            
            if (originalMessage == null)
                return BadRequest(new { error = "No customer messages found in conversation" });

            // Build reply subject - always use the original conversation subject, not from individual messages
            // Remove any existing "Re:" prefixes to avoid "Re: Re: ..."
            var originalSubject = conversation.Subject;
            var replySubject = originalSubject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ||
                              originalSubject.StartsWith("SV:", StringComparison.OrdinalIgnoreCase)
                ? originalSubject
                : $"Re: {originalSubject}";

            // Use the body as-is from the client (signature is already included if user wants it)
            var replyBody = dto.Body;

            // Convert HTML body to text if needed (for bodyText parameter)
            var bodyText = StripHtmlTags(replyBody);

            // Send the reply via Graph API
            // Always reply to the customer (conversation.FromEmail) - AI messages are just context
            var sentMessageId = await _graphService.SendReplyAsync(
                fromEmail: emailMapping.EmailAddress,
                fromName: emailMapping.DisplayName ?? currentUser.Name,
                toEmail: conversation.FromEmail, // Customer's email
                toName: conversation.FromName,   // Customer's name
                subject: replySubject,
                bodyHtml: replyBody,
                bodyText: bodyText,
                inReplyToMessageId: originalMessage.GraphMessageId, // Original customer message
                conversationId: conversation.GraphConversationId,
                cc: dto.Cc,
                bcc: dto.Bcc,
                ct: default);

            if (string.IsNullOrEmpty(sentMessageId))
                return StatusCode(500, new { error = "Failed to send email" });

            // Save the sent message to the database
            var sentMessage = new EmailMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                GraphMessageId = sentMessageId,
                GraphConversationId = conversation.GraphConversationId,
                InReplyToId = originalMessage.GraphMessageId,
                Subject = replySubject,
                FromEmail = emailMapping.EmailAddress,
                FromName = emailMapping.DisplayName ?? currentUser.Name,
                ToEmail = conversation.FromEmail,
                ToName = conversation.FromName,
                Cc = dto.Cc,
                Bcc = dto.Bcc,
                ReceivedDateTime = DateTime.UtcNow,
                SentDateTime = DateTime.UtcNow,
                IsRead = true,
                IsOutgoing = true,
                IsSentFromSystem = true,
                SentByUserId = currentUser.Id,
                HasAttachments = false,
                AttachmentCount = 0,
                MessageBody = replyBody,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailMessages.Add(sentMessage);

            // Update conversation
            conversation.MessageCount += 1;
            conversation.LastMessageReceivedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.Status = "InProgress"; // Mark as in progress when reply is sent

            await _context.SaveChangesAsync();

            // Reload conversation with updated data
            var updatedConversation = await _context.EmailConversations
                .Include(c => c.AssignedTo)
                .Include(c => c.ExtractedData)
                .Include(c => c.Messages.OrderBy(m => m.ReceivedDateTime))
                    .ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (updatedConversation == null)
                return NotFound();

            // Get queue status for all messages
            var messageIds = updatedConversation.Messages.Select(m => m.Id).ToList();
            var queueItems = await _context.EmailClassificationQueues
                .Where(q => messageIds.Contains(q.MessageId))
                .ToListAsync();

            var queueDict = queueItems.ToDictionary(q => q.MessageId);

            var conversationDto = MapToConversationDto(updatedConversation);
            conversationDto.Messages = updatedConversation.Messages.Select(m => MapToMessageDto(m, queueDict.GetValueOrDefault(m.Id))).ToList();

            return Ok(conversationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to conversation {Id}", id);
            return StatusCode(500, new { error = "Error sending reply", details = ex.Message });
        }
    }

    private string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Simple HTML tag removal (for basic cases)
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        return text.Trim();
    }
}

public class AssignConversationDto
{
    public Guid? UserId { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class ReplyToConversationDto
{
    public string Body { get; set; } = string.Empty;
    public string? FromEmailAddress { get; set; } // Optional: if not provided, uses default email
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
}

