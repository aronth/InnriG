using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class EmailPollingBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailPollingBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private Timer? _timer;
    private DateTime? _lastPollTime;

    public EmailPollingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EmailPollingBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!EmailConfigurationHelper.IsEmailConfigured(_configuration))
        {
            _logger.LogWarning("Email configuration is incomplete. EmailPollingBackgroundService will not start.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("EmailPollingBackgroundService is starting.");
        
        var pollIntervalMinutes = _configuration.GetValue<int>("Email:PollIntervalMinutes", 5);
        
        // Start immediately, then repeat every N minutes
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(pollIntervalMinutes));
        
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            // Double-check configuration before processing
            if (!EmailConfigurationHelper.IsEmailConfigured(_configuration))
            {
                _logger.LogWarning("Email configuration is incomplete. Skipping email polling.");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var graphService = scope.ServiceProvider.GetRequiredService<IGraphEmailService>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var queueService = scope.ServiceProvider.GetRequiredService<IEmailClassificationQueueService>();
            var junkFilterService = scope.ServiceProvider.GetRequiredService<IEmailJunkFilterService>();

            // Determine since when to poll
            DateTime? since = _lastPollTime;
            if (!since.HasValue)
            {
                // First poll: get last message time from database, or default to 24 hours ago
                var lastMessage = await context.EmailMessages
                    .OrderByDescending(m => m.ReceivedDateTime)
                    .FirstOrDefaultAsync();

                since = lastMessage?.ReceivedDateTime ?? DateTime.UtcNow.AddHours(-24);
            }

            _logger.LogInformation("Polling emails since {Since}", since);

            // Get new messages from Graph API
            var graphMessages = await graphService.GetMessagesAsync(since);

            if (graphMessages.Count == 0)
            {
                _logger.LogDebug("No new messages found");
                _lastPollTime = DateTime.UtcNow;
                return;
            }

            _logger.LogInformation("Found {Count} new messages", graphMessages.Count);

            // Process each message
            foreach (var graphMsg in graphMessages)
            {
                try
                {
                    // Check if message already exists
                    var existingMessage = await context.EmailMessages
                        .FirstOrDefaultAsync(m => m.GraphMessageId == graphMsg.Id);

                    if (existingMessage != null)
                    {
                        _logger.LogDebug("Message {MessageId} already exists, skipping", graphMsg.Id);
                        continue;
                    }

                    // Check if email matches junk filter
                    var isJunk = await junkFilterService.IsJunkEmailAsync(graphMsg.Subject, graphMsg.FromEmail);
                    if (isJunk)
                    {
                        _logger.LogInformation("Message {MessageId} from {FromEmail} with subject '{Subject}' matched junk filter, skipping", 
                            graphMsg.Id, graphMsg.FromEmail, graphMsg.Subject);
                        continue;
                    }

                    // Find or create conversation
                    var conversation = await FindOrCreateConversationAsync(context, graphMsg);

                    // Create email message
                    var emailMessage = new EmailMessage
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        GraphMessageId = graphMsg.Id,
                        GraphConversationId = graphMsg.ConversationId,
                        InReplyToId = graphMsg.InReplyTo,
                        Subject = graphMsg.Subject,
                        FromEmail = graphMsg.FromEmail,
                        FromName = graphMsg.FromName,
                        ToEmail = graphMsg.ToEmail,
                        ToName = graphMsg.ToName,
                        Cc = graphMsg.Cc,
                        Bcc = graphMsg.Bcc,
                        ReceivedDateTime = graphMsg.ReceivedDateTime,
                        SentDateTime = graphMsg.SentDateTime,
                        IsRead = graphMsg.IsRead,
                        IsOutgoing = false,
                        IsSentFromSystem = false,
                        HasAttachments = graphMsg.HasAttachments,
                        AttachmentCount = graphMsg.AttachmentCount,
                        Importance = graphMsg.Importance,
                        Flag = graphMsg.Flag,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.EmailMessages.Add(emailMessage);

                    // Update conversation
                    conversation.MessageCount++;
                    conversation.LastMessageId = graphMsg.Id;
                    conversation.LastMessageReceivedAt = graphMsg.ReceivedDateTime;
                    conversation.UpdatedAt = DateTime.UtcNow;

                    // Queue for classification
                    await queueService.EnqueueClassificationAsync(emailMessage.Id);

                    _logger.LogDebug("Processed message {MessageId} in conversation {ConversationId}",
                        graphMsg.Id, conversation.GraphConversationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message {MessageId}", graphMsg.Id);
                    // Continue with next message
                }
            }

            await context.SaveChangesAsync();
            _lastPollTime = DateTime.UtcNow;

            _logger.LogInformation("Completed email polling: processed {Count} messages", graphMessages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in email polling background service");
        }
    }

    private async Task<EmailConversation> FindOrCreateConversationAsync(
        AppDbContext context,
        GraphMessageInfo graphMsg)
    {
        // Try to find by GraphConversationId first
        var conversation = await context.EmailConversations
            .FirstOrDefaultAsync(c => c.GraphConversationId == graphMsg.ConversationId);

        if (conversation != null)
            return conversation;

        // Create new conversation
        conversation = new EmailConversation
        {
            Id = Guid.NewGuid(),
            GraphConversationId = graphMsg.ConversationId,
            GraphThreadId = graphMsg.ThreadId,
            Subject = NormalizeSubject(graphMsg.Subject),
            FromEmail = graphMsg.FromEmail,
            FromName = graphMsg.FromName,
            Status = "New",
            Priority = "Normal",
            MessageCount = 0,
            LastMessageReceivedAt = graphMsg.ReceivedDateTime,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.EmailConversations.Add(conversation);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created new conversation {ConversationId} for subject: {Subject}",
            conversation.GraphConversationId, conversation.Subject);

        return conversation;
    }

    private string NormalizeSubject(string subject)
    {
        if (string.IsNullOrEmpty(subject))
            return string.Empty;

        // Remove common prefixes
        var normalized = subject.Trim();
        var prefixes = new[] { "Re:", "RE:", "Fwd:", "FWD:", "FW:" };
        
        foreach (var prefix in prefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(prefix.Length).Trim();
            }
        }

        return normalized;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EmailPollingBackgroundService is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

