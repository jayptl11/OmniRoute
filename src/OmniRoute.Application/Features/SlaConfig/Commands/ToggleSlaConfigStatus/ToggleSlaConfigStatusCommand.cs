using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.SlaConfig.Commands.ToggleSlaConfigStatus;

public record ToggleSlaConfigStatusCommand(Guid Id, bool IsActive) : ICommand;
