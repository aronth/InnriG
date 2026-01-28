using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace InnriGreifi.API.Services;

public class EmailClassificationBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailClassificationBackgroundService> _logger;
    private readonly Channel<Guid> _queue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _processingTask;
    private bool _isRunning = false;

    public EmailClassificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EmailClassificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();

        // Create unbounded channel for queue items
        var options = new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        };
        _queue = Channel.CreateUnbounded<Guid>(options);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Check if email is configured - classification can work without email polling
        // but we still need to check if services are available
        _logger.LogInformation("EmailClassificationBackgroundService is starting.");

        // Reconstruct queue from database
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IEmailClassificationQueueService>();
        await queueService.ReconstructQueueOnStartupAsync();

        // Load pending items into memory queue
        await LoadPendingItemsIntoQueueAsync();

        // Start processing task
        _isRunning = true;
        _processingTask = Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token), cancellationToken);

        _logger.LogInformation("EmailClassificationBackgroundService started.");
    }

    public async Task EnqueueMessageAsync(Guid messageId)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Background service not running, message {MessageId} will be processed on next startup", messageId);
            return;
        }

        try
        {
            await _queue.Writer.WriteAsync(messageId);
            _logger.LogInformation("Added message {MessageId} to in-memory queue for immediate processing", messageId);
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Queue writer is closed, message {MessageId} will be processed on next startup", messageId);
        }
    }

    private async Task LoadPendingItemsIntoQueueAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendingItems = await context.EmailClassificationQueues
                .Where(q => q.Status == "Pending")
                .OrderBy(q => q.QueuedAt)
                .Select(q => q.MessageId)
                .ToListAsync();

            foreach (var messageId in pendingItems)
            {
                await _queue.Writer.WriteAsync(messageId);
            }

            _logger.LogInformation("Loaded {Count} pending items into queue", pendingItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending items into queue");
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Classification queue processor started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for item or timeout
                var hasItem = await _queue.Reader.WaitToReadAsync(cancellationToken);
                if (!hasItem)
                    continue;

                if (!_queue.Reader.TryRead(out var messageId))
                    continue;

                _logger.LogInformation("Processing queued message {MessageId} from classification queue", messageId);
                
                // Process item
                await ProcessQueueItemAsync(messageId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in classification queue processor");
                // Wait a bit before retrying
                await Task.Delay(5000, cancellationToken);
            }
        }

        _logger.LogInformation("Classification queue processor stopped");
    }

    private async Task ProcessQueueItemAsync(Guid messageId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IEmailClassificationQueueService>();
        var classificationService = scope.ServiceProvider.GetRequiredService<IEmailClassificationService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var junkFilterService = scope.ServiceProvider.GetRequiredService<IEmailJunkFilterService>();

        // Find queue item
        var queueItem = await context.EmailClassificationQueues
            .FirstOrDefaultAsync(q => q.MessageId == messageId, cancellationToken);

        if (queueItem == null)
        {
            _logger.LogWarning("Queue item not found for message {MessageId}", messageId);
            return;
        }

        // Get message to find GraphMessageId
        var message = await context.EmailMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message == null)
        {
            _logger.LogWarning("Message {MessageId} not found", messageId);
            await queueService.MarkFailedAsync(queueItem.Id, "Message not found", false);
            return;
        }

        // Check if email matches junk filter (double-check in case filter was added after queuing)
        var isJunk = await junkFilterService.IsJunkEmailAsync(message.Subject, message.FromEmail, cancellationToken);
        if (isJunk)
        {
            _logger.LogInformation("Message {MessageId} from {FromEmail} with subject '{Subject}' matched junk filter, marking as skipped", 
                messageId, message.FromEmail, message.Subject);
            await queueService.MarkFailedAsync(queueItem.Id, "Email matched junk filter", false);
            return;
        }

        try
        {
            _logger.LogInformation("Starting classification for message {MessageId} (GraphMessageId: {GraphMessageId})", 
                messageId, message.GraphMessageId);

            // Mark as processing
            await queueService.MarkProcessingAsync(queueItem.Id);

            // Classify email
            var result = await classificationService.ClassifyEmailAsync(message.GraphMessageId, cancellationToken);

            // Mark as completed
            await queueService.MarkCompletedAsync(queueItem.Id);

            _logger.LogInformation("Classified message {MessageId} as {Classification} (confidence: {Confidence})",
                message.GraphMessageId, result.Classification, result.Confidence);

            // Initialize workflow if one exists for this classification
            try
            {
                var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionService>();
                var conversation = await context.EmailConversations
                    .FirstOrDefaultAsync(c => c.Id == message.ConversationId, cancellationToken);

                if (conversation != null && !string.IsNullOrEmpty(result.Classification))
                {
                    await workflowService.InitializeWorkflowAsync(conversation.Id, result.Classification, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error initializing workflow for conversation {ConversationId}", message.ConversationId);
                // Don't fail classification if workflow initialization fails
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying message {MessageId}", message.GraphMessageId);
            var shouldRetry = queueItem.RetryCount < 3;
            await queueService.MarkFailedAsync(queueItem.Id, ex.Message, shouldRetry);

            // Re-queue if should retry
            if (shouldRetry)
            {
                await _queue.Writer.WriteAsync(messageId, cancellationToken);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EmailClassificationBackgroundService is stopping.");

        _isRunning = false;
        _cancellationTokenSource.Cancel();
        _queue.Writer.Complete();

        if (_processingTask != null)
        {
            await _processingTask;
        }

        _logger.LogInformation("EmailClassificationBackgroundService stopped.");
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }
}

