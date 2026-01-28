namespace InnriGreifi.API.Services;

public interface IEmailJunkFilterService
{
    Task<bool> IsJunkEmailAsync(string? subject, string? senderEmail, CancellationToken ct = default);
}

