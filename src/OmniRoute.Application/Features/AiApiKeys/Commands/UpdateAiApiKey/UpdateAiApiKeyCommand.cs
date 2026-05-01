using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.UpdateAiApiKey;

public record UpdateAiApiKeyCommand(
    Guid Id,
    string DisplayName,
    string? PlainKeyValue,   // null = keep existing
    int Priority) : ICommand;
