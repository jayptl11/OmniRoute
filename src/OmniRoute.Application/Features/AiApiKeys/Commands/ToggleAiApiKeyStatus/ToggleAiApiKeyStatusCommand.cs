using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.ToggleAiApiKeyStatus;

public record ToggleAiApiKeyStatusCommand(Guid Id) : ICommand;
