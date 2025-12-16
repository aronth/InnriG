using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class WaitTimeMonitoringService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WaitTimeMonitoringService> _logger;
    private Timer? _timer;
    private const int CheckIntervalMinutes = 5;
    private readonly TimeSpan OpeningTime = new TimeSpan(11, 30, 0); // 11:30 UTC
    private readonly TimeSpan ClosingTime = new TimeSpan(22, 0, 0); // 22:00 UTC
    private readonly TimeSpan GreifinnSkipStart = new TimeSpan(10, 0, 0); // 10:00 UTC
    private readonly TimeSpan GreifinnSkipEnd = new TimeSpan(11, 30, 0); // 11:30 UTC

    public WaitTimeMonitoringService(IServiceProvider serviceProvider, ILogger<WaitTimeMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WaitTimeMonitoringService is starting.");
        
        // Start immediately, then repeat every 5 minutes
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(CheckIntervalMinutes));
        
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var currentTime = now.TimeOfDay;

            // Check if we're within opening hours (11:30 - 22:00 UTC)
            if (currentTime < OpeningTime || currentTime >= ClosingTime)
            {
                _logger.LogDebug("Outside opening hours (11:30-22:00 UTC), skipping check");
                return;
            }

            // Check if we should skip Greifinn (10:00-11:30 UTC)
            bool skipGreifinn = currentTime >= GreifinnSkipStart && currentTime < GreifinnSkipEnd;

            using var scope = _serviceProvider.CreateScope();
            var scraper = scope.ServiceProvider.GetRequiredService<IWaitTimeScraper>();
            var pushoverService = scope.ServiceProvider.GetRequiredService<IPushoverService>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Scrape both restaurants
            var tasks = new List<Task<Models.DTOs.WaitTimeResultDto>>();
            
            if (!skipGreifinn)
            {
                tasks.Add(scraper.ScrapeGreifinnAsync());
            }
            else
            {
                _logger.LogDebug("Skipping Greifinn check (10:00-11:30 UTC)");
            }
            
            tasks.Add(scraper.ScrapeSpretturinnAsync());

            var results = await Task.WhenAll(tasks);

            // Store records in database
            foreach (var result in results)
            {
                if (result.Success)
                {
                    var record = new WaitTimeRecord
                    {
                        Id = Guid.NewGuid(),
                        Restaurant = result.Restaurant,
                        SottMinutes = result.SottMinutes,
                        SentMinutes = result.SentMinutes,
                        IsClosed = result.IsClosed,
                        ScrapedAt = result.ScrapedAt
                    };

                    context.WaitTimeRecords.Add(record);
                }
                else
                {
                    _logger.LogWarning("Failed to scrape {Restaurant}: {Error}", 
                        result.RestaurantName, result.ErrorMessage);
                }
            }

            await context.SaveChangesAsync();

            // Check notifications for each result
            foreach (var result in results)
            {
                if (!result.Success || result.IsClosed)
                    continue;

                await CheckAndSendNotificationsAsync(context, pushoverService, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WaitTimeMonitoringService DoWork");
        }
    }

    private async Task CheckAndSendNotificationsAsync(
        AppDbContext context,
        IPushoverService pushoverService,
        Models.DTOs.WaitTimeResultDto result)
    {
        try
        {
            // Get all enabled notifications for this restaurant
            var notifications = await context.WaitTimeNotifications
                .Where(n => n.Restaurant == result.Restaurant && n.IsEnabled)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                // Check Sótt threshold
                if (notification.SottThresholdMinutes.HasValue && 
                    result.SottMinutes.HasValue &&
                    result.SottMinutes.Value >= notification.SottThresholdMinutes.Value)
                {
                    // Check if we already notified for this threshold crossing
                    // We only notify once when crossing the threshold
                    // If LastNotifiedSott is null or the previous value was below threshold, send notification
                    var previousRecord = await context.WaitTimeRecords
                        .Where(r => r.Restaurant == result.Restaurant && r.SottMinutes.HasValue)
                        .OrderByDescending(r => r.ScrapedAt)
                        .Skip(1) // Skip the current one we just added
                        .FirstOrDefaultAsync();

                    bool shouldNotify = false;
                    if (previousRecord == null || !previousRecord.SottMinutes.HasValue)
                    {
                        // No previous record or previous was null, this is first crossing
                        shouldNotify = true;
                    }
                    else if (previousRecord.SottMinutes.Value < notification.SottThresholdMinutes.Value)
                    {
                        // Previous was below threshold, now above - crossing threshold
                        shouldNotify = true;
                    }

                    if (shouldNotify && 
                        (notification.LastNotifiedSott == null || 
                         notification.LastNotifiedSott.Value < DateTime.UtcNow.AddMinutes(-CheckIntervalMinutes * 2)))
                    {
                        var message = $"Sótt biðtími hjá {result.RestaurantName} er nú {result.SottMinutes} mínútur (þröskuldur: {notification.SottThresholdMinutes} mín)";
                        var title = $"Biðtími yfir þröskuldi - {result.RestaurantName}";

                        var sent = await pushoverService.SendNotificationAsync(
                            notification.PushoverUserKey,
                            message,
                            title);

                        if (sent)
                        {
                            notification.LastNotifiedSott = DateTime.UtcNow;
                            await context.SaveChangesAsync();
                            _logger.LogInformation("Sent Sótt notification for {Restaurant} to user {UserId}", 
                                result.RestaurantName, notification.UserId);
                        }
                    }
                }

                // Check Sent/Heimsent threshold
                if (notification.SentThresholdMinutes.HasValue && 
                    result.SentMinutes.HasValue &&
                    result.SentMinutes.Value >= notification.SentThresholdMinutes.Value)
                {
                    // Check if we already notified for this threshold crossing
                    var previousRecord = await context.WaitTimeRecords
                        .Where(r => r.Restaurant == result.Restaurant && r.SentMinutes.HasValue)
                        .OrderByDescending(r => r.ScrapedAt)
                        .Skip(1) // Skip the current one we just added
                        .FirstOrDefaultAsync();

                    bool shouldNotify = false;
                    if (previousRecord == null || !previousRecord.SentMinutes.HasValue)
                    {
                        // No previous record or previous was null, this is first crossing
                        shouldNotify = true;
                    }
                    else if (previousRecord.SentMinutes.Value < notification.SentThresholdMinutes.Value)
                    {
                        // Previous was below threshold, now above - crossing threshold
                        shouldNotify = true;
                    }

                    if (shouldNotify && 
                        (notification.LastNotifiedSent == null || 
                         notification.LastNotifiedSent.Value < DateTime.UtcNow.AddMinutes(-CheckIntervalMinutes * 2)))
                    {
                        var message = $"Sent/Heimsent biðtími hjá {result.RestaurantName} er nú {result.SentMinutes} mínútur (þröskuldur: {notification.SentThresholdMinutes} mín)";
                        var title = $"Biðtími yfir þröskuldi - {result.RestaurantName}";

                        var sent = await pushoverService.SendNotificationAsync(
                            notification.PushoverUserKey,
                            message,
                            title);

                        if (sent)
                        {
                            notification.LastNotifiedSent = DateTime.UtcNow;
                            await context.SaveChangesAsync();
                            _logger.LogInformation("Sent Sent/Heimsent notification for {Restaurant} to user {UserId}", 
                                result.RestaurantName, notification.UserId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and sending notifications for {Restaurant}", result.RestaurantName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("WaitTimeMonitoringService is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
