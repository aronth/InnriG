using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services;

public interface IEmailClassificationQueueService
{
    Task EnqueueClassificationAsync(Guid messageId);
    Task<EmailClassificationQueue?> DequeueAsync();
    Task MarkProcessingAsync(Guid queueItemId);
    Task MarkCompletedAsync(Guid queueItemId);
    Task MarkFailedAsync(Guid queueItemId, string errorMessage, bool shouldRetry);
    Task ReconstructQueueOnStartupAsync();
    Task<int> GetPendingCountAsync();
}

