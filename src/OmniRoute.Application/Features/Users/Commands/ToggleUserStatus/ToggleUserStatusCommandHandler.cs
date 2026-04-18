using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Users.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Users.Commands.ToggleUserStatus;

internal sealed class ToggleUserStatusCommandHandler
    : ICommandHandler<ToggleUserStatusCommand, ToggleUserStatusResponse>
{
    private readonly IApplicationDbContext _db;

    public ToggleUserStatusCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ToggleUserStatusResponse>> Handle(
        ToggleUserStatusCommand command,
        CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);

        if (user is null)
            return Result<ToggleUserStatusResponse>.Failure("USER_NOT_FOUND", "User not found.");

        var terminalStatuses = new[]
        {
            LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled
        };

        var activeLeadCount = await _db.Leads
            .CountAsync(l => l.AssignedUserId == command.UserId
                          && !terminalStatuses.Contains(l.Status), ct);

        if (command.IsActive)
            user.Activate();
        else
        {
            user.Deactivate();
            // Revoke all active refresh tokens so existing sessions are terminated
            var now = DateTime.UtcNow;
            foreach (var token in user.RefreshTokens.Where(t => t.RevokedAt == null))
                token.RevokedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        return Result<ToggleUserStatusResponse>.Success(
            new ToggleUserStatusResponse(user.UserId, user.IsActive, activeLeadCount));
    }
}
