using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.Teams.Commands.CreateTeam;

internal sealed class CreateTeamCommandHandler : ICommandHandler<CreateTeamCommand, Guid>
{
    private readonly ITeamRepository _repository;
    private readonly IApplicationDbContext _db;

    public CreateTeamCommandHandler(ITeamRepository repository, IApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateTeamCommand command, CancellationToken ct)
    {
        var team = Team.Create(command.TeamName, command.TeamType, command.LeaderId, command.StoreId);

        await _repository.AddAsync(team, ct);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(team.Id);
    }
}
