using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Teams.Queries.GetTeamById;

internal sealed class GetTeamByIdQueryHandler : IQueryHandler<GetTeamByIdQuery, TeamDto>
{
    private readonly ITeamRepository _repository;

    public GetTeamByIdQueryHandler(ITeamRepository repository) => _repository = repository;

    public async Task<Result<TeamDto>> Handle(GetTeamByIdQuery query, CancellationToken ct)
    {
        var team = await _repository.GetByIdAsync(query.Id, ct);
        if (team is null)
            return Result<TeamDto>.Failure("NOT_FOUND", "Team not found.");

        return Result<TeamDto>.Success(new TeamDto(
            team.Id, team.TeamName, team.TeamType.ToString(),
            team.LeaderId, team.StoreId, team.IsActive, team.CreatedAt));
    }
}
