using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Teams.Queries.GetTeams;

public record GetTeamsQuery(AssignedGroup? TeamType, Guid? StoreId, bool? IsActive) : IQuery<List<TeamDto>>;
