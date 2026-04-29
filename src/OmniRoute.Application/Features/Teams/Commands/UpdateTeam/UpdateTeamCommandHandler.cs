using Microsoft.EntityFrameworkCore;
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

        var oldLeaderId = team.LeaderId;
        team.Update(command.TeamName, command.LeaderId, command.StoreId);

        // Nếu trưởng nhóm thay đổi: xóa TeamId của TN cũ, gán TeamId cho TN mới
        if (oldLeaderId != command.LeaderId)
        {
            if (oldLeaderId.HasValue)
            {
                var oldLeader = await _db.Users.FirstOrDefaultAsync(u => u.UserId == oldLeaderId.Value, ct);
                // Chỉ xóa nếu họ đang ở chính đội này (tránh xóa nếu họ đã được gán sang đội khác)
                if (oldLeader is not null && oldLeader.TeamId == team.Id)
                    oldLeader.AssignToTeam(null);
            }

            if (command.LeaderId.HasValue)
            {
                var newLeader = await _db.Users.FirstOrDefaultAsync(u => u.UserId == command.LeaderId.Value, ct);
                if (newLeader is not null)
                    newLeader.AssignToTeam(team.Id);
            }
        }

        await _repository.UpdateAsync(team, ct);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
