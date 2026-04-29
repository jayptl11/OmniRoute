using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreCapacity;

internal sealed class GetStoreCapacityQueryHandler
    : IQueryHandler<GetStoreCapacityQuery, StoreCapacityResultDto>
{
    private static readonly HashSet<LeadStatus> ActiveStatuses =
    [
        LeadStatus.Assigned,
        LeadStatus.Contacted,
        LeadStatus.InProgress,
        LeadStatus.PendingAssignment,
        LeadStatus.PendingDispatch,
    ];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetStoreCapacityQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<StoreCapacityResultDto>> Handle(GetStoreCapacityQuery query, CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
            return Result<StoreCapacityResultDto>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");

        var store = await _db.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == storeId, ct);

        if (store is null)
            return Result<StoreCapacityResultDto>.Failure("STORE_NOT_FOUND", "Không tìm thấy cửa hàng.");

        var activeLeads = await _db.Leads
            .AsNoTracking()
            .CountAsync(l => l.AssignedStoreId == storeId && ActiveStatuses.Contains(l.Status), ct);

        var availableSlots = Math.Max(0, store.MaxCapacity - activeLeads);
        var isOverCapacity = activeLeads >= store.MaxCapacity;
        // Near capacity: còn < 20% slot trống
        var isNearCapacity = !isOverCapacity && store.MaxCapacity > 0 &&
                             availableSlots < (int)Math.Ceiling(store.MaxCapacity * 0.2);

        return Result<StoreCapacityResultDto>.Success(new StoreCapacityResultDto(
            StoreId: store.Id,
            StoreCode: store.StoreCode,
            StoreName: store.StoreName,
            Address: store.Address,
            Region: store.Region,
            MaxCapacity: store.MaxCapacity,
            ActiveLeads: activeLeads,
            AvailableSlots: availableSlots,
            IsOverCapacity: isOverCapacity,
            IsNearCapacity: isNearCapacity));
    }
}
