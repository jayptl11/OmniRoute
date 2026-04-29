using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.StoreManagement.Commands.UnassignStoreStaff;

internal sealed class UnassignStoreStaffCommandHandler : ICommandHandler<UnassignStoreStaffCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public UnassignStoreStaffCommandHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UnassignStoreStaffCommand command, CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
            return Result.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng.");

        if (user.StoreId != storeId)
            return Result.Failure("USER_NOT_IN_STORE", "Người dùng không thuộc đơn vị của bạn.");

        var terminalStatuses = new[]
        {
            LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled
        };

        var activeLeadCount = await _db.Leads
            .CountAsync(l => l.AssignedUserId == command.UserId && !terminalStatuses.Contains(l.Status), ct);

        if (activeLeadCount > 0)
            return Result.Failure(
                "ACTIVE_LEADS_WARNING",
                $"Người dùng đang có {activeLeadCount} lead chưa hoàn tất. Hãy chuyển giao (reassign) trước khi xóa khỏi đơn vị.");

        user.AssignToStore(null);

        var log = ActivityLog.Create(
            entityType: "USER",
            entityId: command.UserId,
            action: "STORE_STAFF_REMOVED",
            performedBy: _currentUserService.GetUserId(),
            oldValue: storeId.ToString());

        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
