using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.TestAiApiKey;

public record TestAiApiKeyResult(bool Success, string? ErrorMessage, long LatencyMs, string Provider);

public record TestAiApiKeyCommand(Guid Id) : ICommand<TestAiApiKeyResult>;
