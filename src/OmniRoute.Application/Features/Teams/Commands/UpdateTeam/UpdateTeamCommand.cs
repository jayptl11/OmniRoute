using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Teams.Commands.UpdateTeam;

public record UpdateTeamCommand(
    Guid Id,
    string TeamName,
    Guid? LeaderId,
    Guid? StoreId) : ICommand;
