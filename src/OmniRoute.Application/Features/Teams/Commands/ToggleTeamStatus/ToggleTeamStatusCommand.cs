using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Teams.Commands.ToggleTeamStatus;

public record ToggleTeamStatusCommand(Guid Id, bool IsActive) : ICommand;
