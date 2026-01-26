using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services;

public interface IEmailClassificationService
{
    Task<ClassificationResult> ClassifyEmailAsync(string messageId, CancellationToken ct = default);
    Task<ClassificationResult> RegenerateAIAnalysisAsync(Guid messageId, CancellationToken ct = default);
}

public class ClassificationResult
{
    public string Classification { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public EmailExtractedData? ExtractedData { get; set; }
}

