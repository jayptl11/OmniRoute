using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Domain.Constants;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Application.Features.StoreManagement.Commands.AssignStoreStaff;

internal sealed class AssignStoreStaffCommandHandler : ICommandHandler<AssignStoreStaffCommand>
{
    private static readonly HashSet<string> AllowedRoles = [RoleCatalog.StoreSales];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AssignStoreStaffCommandHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(AssignStoreStaffCommand command, CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
        {
            return Result.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");
        }

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId, ct);

        if (store is null)
        {
            return Result.Failure("STORE_NOT_FOUND", "Không tìm thấy cửa hàng.");
        }

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);

        if (user is null)
        {
            return Result.Failure("USER_NOT_FOUND", "Không tìm thấy người dùng.");
        }

        if (!user.IsActive)
        {
            return Result.Failure("USER_INACTIVE", "Người dùng đã bị khóa, không thể thêm vào đơn vị.");
        }

        var userRole = user.Role?.RoleName;

        if (userRole is null || !AllowedRoles.Contains(userRole))
        {
            return Result.Failure(
                "INVALID_ROLE",
                $"Chỉ có thể thêm nhân viên với role {RoleCatalog.StoreSales} ({RoleCatalog.GetDisplayName(RoleCatalog.StoreSales)}) vào đơn vị. Người dùng này có role '{userRole ?? "không xác định"}'.");
        }

        if (user.StoreId == storeId)
        {
            return Result.Failure("ALREADY_IN_STORE", "Người dùng đã là nhân sự của đơn vị này.");
        }

        if (user.StoreId is not null && user.StoreId != storeId)
        {
            return Result.Failure("IN_OTHER_STORE", "Người dùng đang thuộc đơn vị khác. Hãy xóa khỏi đơn vị đó trước.");
        }

        user.AssignToStore(storeId);

        var log = ActivityLog.Create(
            entityType: "USER",
            entityId: command.UserId,
            action: "STORE_STAFF_ADDED",
            performedBy: _currentUserService.GetUserId(),
            newValue: storeId.ToString());

        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
