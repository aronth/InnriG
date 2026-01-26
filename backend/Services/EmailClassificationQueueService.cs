using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class EmailClassificationQueueService : IEmailClassificationQueueService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailClassificationQueueService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const int MaxRetries = 3;

    public EmailClassificationQueueService(
        AppDbContext context, 
        ILogger<EmailClassificationQueueService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task EnqueueClassificationAsync(Guid messageId)
    {
        // Check if already queued
        var existing = await _context.EmailClassificationQueues
            .FirstOrDefaultAsync(q => q.MessageId == messageId && q.Status != "Completed");

        if (existing != null)
        {
            _logger.LogDebug("Message {MessageId} already in queue with status {Status}", messageId, existing.Status);
            return;
        }

        var queueItem = new EmailClassificationQueue
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            Status = "Pending",
            RetryCount = 0,
            QueuedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmailClassificationQueues.Add(queueItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Enqueued message {MessageId} for classification", messageId);

        // Try to add to in-memory queue for immediate processing
        try
        {
            var backgroundService = _serviceProvider.GetService<EmailClassificationBackgroundService>();
            if (backgroundService != null)
            {
                await backgroundService.EnqueueMessageAsync(messageId);
            }
        }
        catch (Exception ex)
        {
            // Background service might not be available yet, item will be processed on startup
            _logger.LogDebug(ex, "Could not add message {MessageId} to in-memory queue, will be processed on startup", messageId);
        }
    }

    public async Task<EmailClassificationQueue?> DequeueAsync()
    {
        var item = await _context.EmailClassificationQueues
            .Where(q => q.Status == "Pending")
            .OrderBy(q => q.QueuedAt)
            .FirstOrDefaultAsync();

        return item;
    }

    public async Task MarkProcessingAsync(Guid queueItemId)
    {
        var item = await _context.EmailClassificationQueues.FindAsync(queueItemId);
        if (item == null)
            return;

        item.Status = "Processing";
        item.StartedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogDebug("Marked queue item {QueueItemId} as Processing", queueItemId);
    }

    public async Task MarkCompletedAsync(Guid queueItemId)
    {
        var item = await _context.EmailClassificationQueues.FindAsync(queueItemId);
        if (item == null)
            return;

        item.Status = "Completed";
        item.CompletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogDebug("Marked queue item {QueueItemId} as Completed", queueItemId);
    }

    public async Task MarkFailedAsync(Guid queueItemId, string errorMessage, bool shouldRetry)
    {
        var item = await _context.EmailClassificationQueues.FindAsync(queueItemId);
        if (item == null)
            return;

        item.RetryCount++;
        item.ErrorMessage = errorMessage;
        item.UpdatedAt = DateTime.UtcNow;

        if (shouldRetry && item.RetryCount < MaxRetries)
        {
            item.Status = "Pending";
            item.StartedAt = null;
            _logger.LogWarning("Queue item {QueueItemId} failed, retrying (attempt {RetryCount}/{MaxRetries}): {Error}",
                queueItemId, item.RetryCount, MaxRetries, errorMessage);
        }
        else
        {
            item.Status = "Failed";
            item.CompletedAt = DateTime.UtcNow;
            _logger.LogError("Queue item {QueueItemId} failed after {RetryCount} attempts: {Error}",
                queueItemId, item.RetryCount, errorMessage);
        }

        await _context.SaveChangesAsync();
    }

    public async Task ReconstructQueueOnStartupAsync()
    {
        // Find all items that were processing (assume app crashed)
        var stuckItems = await _context.EmailClassificationQueues
            .Where(q => q.Status == "Processing")
            .ToListAsync();

        foreach (var item in stuckItems)
        {
            item.Status = "Pending";
            item.StartedAt = null;
            // Don't increment retry count - it was a crash, not a failure
        }

        var pendingCount = await _context.EmailClassificationQueues
            .Where(q => q.Status == "Pending")
            .CountAsync();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reconstructed queue: {StuckCount} stuck items reset, {PendingCount} pending items",
            stuckItems.Count, pendingCount);
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _context.EmailClassificationQueues
            .Where(q => q.Status == "Pending")
            .CountAsync();
    }
}

