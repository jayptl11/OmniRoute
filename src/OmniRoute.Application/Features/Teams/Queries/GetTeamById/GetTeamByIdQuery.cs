using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Teams.DTOs;

namespace OmniRoute.Application.Features.Teams.Queries.GetTeamById;

public record GetTeamByIdQuery(Guid Id) : IQuery<TeamDto>;
