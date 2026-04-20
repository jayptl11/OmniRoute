using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Teams.Commands.UpdateTeam;

internal sealed class UpdateTeamCommandHandler : ICommandHandler<UpdateTeamCommand>
{
    private readonly ITeamRepository _repository;
    private readonly IApplicationDbContext _db;

    public UpdateTeamCommandHandler(ITeamRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(UpdateTeamCommand command, CancellationToken ct)
    {
        var team = await _repository.GetByIdAsync(command.Id, ct);
        if (team is null)
            return Result.Failure("NOT_FOUND", "Team not found.");

        team.Update(command.TeamName, command.LeaderId, command.StoreId);
        await _repository.UpdateAsync(team, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
