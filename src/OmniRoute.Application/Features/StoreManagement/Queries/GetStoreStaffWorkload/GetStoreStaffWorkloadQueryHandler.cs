using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreStaffWorkload;

internal sealed class GetStoreStaffWorkloadQueryHandler
    : IQueryHandler<GetStoreStaffWorkloadQuery, List<StoreStaffWorkloadDto>>
{
    private static readonly HashSet<LeadStatus> CompletedStatuses =
    [
        LeadStatus.Won,
        LeadStatus.Lost,
        LeadStatus.Cancelled,
    ];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetStoreStaffWorkloadQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<StoreStaffWorkloadDto>>> Handle(
        GetStoreStaffWorkloadQuery query,
        CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
            return Result<List<StoreStaffWorkloadDto>>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");

        var staff = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.StoreId == storeId)
            .Select(u => new
            {
                u.UserId,
                FullName = (u.FirstName + " " + u.LastName).Trim(),
                RoleName = u.Role != null ? u.Role.RoleName : null,
                u.IsActive,
                u.CurrentWorkload
            })
            .ToListAsync(ct);

        if (staff.Count == 0)
            return Result<List<StoreStaffWorkloadDto>>.Success([]);

        var staffIds = staff.Select(s => s.UserId).ToList();

        // Count SLA-violated leads per staff member (active leads only)
        var slaViolatedCounts = await _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedUserId.HasValue &&
                        staffIds.Contains(l.AssignedUserId.Value) &&
                        l.SlaViolated &&
                        !CompletedStatuses.Contains(l.Status))
            .GroupBy(l => l.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        // Count completed leads per staff member
        var completedCounts = await _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedUserId.HasValue &&
                        staffIds.Contains(l.AssignedUserId.Value) &&
                        CompletedStatuses.Contains(l.Status))
            .GroupBy(l => l.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var result = staff.Select(s => new StoreStaffWorkloadDto(
            UserId: s.UserId,
            FullName: s.FullName,
            RoleName: s.RoleName,
            IsActive: s.IsActive,
            CurrentWorkload: s.CurrentWorkload,
            SlaViolatedCount: slaViolatedCounts.GetValueOrDefault(s.UserId, 0),
            CompletedCount: completedCounts.GetValueOrDefault(s.UserId, 0)))
            .ToList();

        return Result<List<StoreStaffWorkloadDto>>.Success(result);
    }
}
