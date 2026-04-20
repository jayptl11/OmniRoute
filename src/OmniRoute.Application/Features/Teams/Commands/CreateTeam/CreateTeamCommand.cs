using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Teams.Commands.CreateTeam;

public record CreateTeamCommand(
    string TeamName,
    AssignedGroup TeamType,
    Guid? LeaderId,
    Guid? StoreId) : ICommand<Guid>;
