using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Teams.Commands.ToggleTeamStatus;

internal sealed class ToggleTeamStatusCommandHandler : ICommandHandler<ToggleTeamStatusCommand>
{
    private readonly ITeamRepository _repository;
    private readonly IApplicationDbContext _db;

    public ToggleTeamStatusCommandHandler(ITeamRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result> Handle(ToggleTeamStatusCommand command, CancellationToken ct)
    {
        var team = await _repository.GetByIdAsync(command.Id, ct);
        if (team is null)
            return Result.Failure("NOT_FOUND", "Team not found.");

        if (command.IsActive)
            team.Activate();
        else
            team.Deactivate();

        await _repository.UpdateAsync(team, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
