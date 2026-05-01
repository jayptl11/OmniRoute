using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.TestLeadClassification;

public record TestLeadClassificationResult(
    bool Success,
    string? NeedType,
    double ConfidenceScore,
    string Reasoning,
    string? AssignedGroup,
    string Provider,
    long LatencyMs,
    string? ErrorMessage);

public record TestLeadClassificationCommand(
    Guid Id,
    string NeedDescription,
    string Channel) : ICommand<TestLeadClassificationResult>;
