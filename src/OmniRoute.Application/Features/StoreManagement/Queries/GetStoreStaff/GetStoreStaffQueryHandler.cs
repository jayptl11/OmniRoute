using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreStaff;

internal sealed class GetStoreStaffQueryHandler
    : IQueryHandler<GetStoreStaffQuery, List<StoreStaffDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetStoreStaffQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<StoreStaffDto>>> Handle(GetStoreStaffQuery query, CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
            return Result<List<StoreStaffDto>>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");

        var staff = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.StoreId == storeId)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new StoreStaffDto(
                u.UserId,
                (u.FirstName + " " + u.LastName).Trim(),
                u.Role != null ? u.Role.RoleName : null,
                u.IsActive,
                u.CurrentWorkload,
                u.LastAssignedAt))
            .ToListAsync(ct);

        return Result<List<StoreStaffDto>>.Success(staff);
    }
}
