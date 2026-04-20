using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.SlaConfig.Commands.UpdateSlaConfig;

public record UpdateSlaConfigCommand(
    Guid Id,
    int MaxHours,
    int WarningBeforeHours) : ICommand;
