using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Teams.Queries.GetTeams;

internal sealed class GetTeamsQueryHandler : IQueryHandler<GetTeamsQuery, List<TeamDto>>
{
    private readonly ITeamRepository _repository;

    public GetTeamsQueryHandler(ITeamRepository repository) => _repository = repository;

    public async Task<Result<List<TeamDto>>> Handle(GetTeamsQuery query, CancellationToken ct)
    {
        var teams = await _repository.GetAllAsync(query.TeamType, query.StoreId, query.IsActive, ct);
        var dtos = teams.Select(t => new TeamDto(
            t.Id, t.TeamName, t.TeamType.ToString(), t.LeaderId, t.StoreId, t.IsActive, t.CreatedAt))
            .ToList();
        return Result<List<TeamDto>>.Success(dtos);
    }
}
