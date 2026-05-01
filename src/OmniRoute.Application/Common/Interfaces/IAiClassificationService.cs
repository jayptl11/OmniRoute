using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Common.Interfaces;

public record AiClassificationResult(
    NeedType NeedType,
    double ConfidenceScore,
    string Reasoning,
    string UsedProvider
);

public interface IAiClassificationService
{
    Task<AiClassificationResult?> ClassifyAsync(
        string needDescription,
        string channel,
        CancellationToken ct = default);

    Task<AiClassificationResult> ClassifyWithKeyAsync(
        string provider,
        string plainKey,
        string needDescription,
        string channel,
        CancellationToken ct = default);
}
