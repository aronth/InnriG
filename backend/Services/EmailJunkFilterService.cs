using InnriGreifi.API.Data;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class EmailJunkFilterService : IEmailJunkFilterService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailJunkFilterService> _logger;

    public EmailJunkFilterService(
        AppDbContext context,
        ILogger<EmailJunkFilterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsJunkEmailAsync(string? subject, string? senderEmail, CancellationToken ct = default)
    {
        // Get all active junk filters
        var filters = await _context.EmailJunkFilters
            .Where(f => f.IsActive)
            .ToListAsync(ct);

        if (filters.Count == 0)
            return false;

        // Normalize inputs for comparison
        var normalizedSubject = NormalizeString(subject);
        var normalizedSender = NormalizeString(senderEmail);

        // Check if any filter matches
        foreach (var filter in filters)
        {
            var filterSubject = NormalizeString(filter.Subject);
            var filterSender = NormalizeString(filter.SenderEmail);

            // Match logic:
            // - If filter field is null, it matches any value (wildcard)
            // - Subject: uses Contains for partial matching (case-insensitive)
            // - Sender: uses exact match (case-insensitive)
            bool subjectMatches = filterSubject == null || 
                                  (normalizedSubject != null && normalizedSubject.Contains(filterSubject, StringComparison.OrdinalIgnoreCase));
            bool senderMatches = filterSender == null || 
                                (normalizedSender != null && normalizedSender.Equals(filterSender, StringComparison.OrdinalIgnoreCase));

            // Both conditions must match for the filter to apply
            if (subjectMatches && senderMatches)
            {
                _logger.LogInformation("Email matched junk filter {FilterId}: Subject='{Subject}', Sender='{Sender}'", 
                    filter.Id, filter.Subject ?? "*", filter.SenderEmail ?? "*");
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        return value.Trim();
    }
}

