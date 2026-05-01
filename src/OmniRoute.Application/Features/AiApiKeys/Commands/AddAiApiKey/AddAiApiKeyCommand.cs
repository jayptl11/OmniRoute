using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.AddAiApiKey;

public record AddAiApiKeyCommand(
    string Provider,
    string DisplayName,
    string PlainKeyValue,
    int Priority) : ICommand<Guid>;
