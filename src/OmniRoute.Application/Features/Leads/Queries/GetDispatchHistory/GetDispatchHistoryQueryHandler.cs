using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetDispatchHistory;

internal sealed class GetDispatchHistoryQueryHandler
    : IQueryHandler<GetDispatchHistoryQuery, List<DispatchHistoryItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetDispatchHistoryQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<DispatchHistoryItemDto>>> Handle(
        GetDispatchHistoryQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        // DP-06: Lấy các lead đã được DP hiện tại phân công (có log DISPATCHED_TO_STORE)
        var dispatched = await _db.ActivityLogs
            .AsNoTracking()
            .Where(al => al.Action == "DISPATCHED_TO_STORE"
                         && al.EntityType == "LEAD"
                         && al.PerformedBy == currentUserId)
            .OrderByDescending(al => al.PerformedAt)
            .Join(_db.Leads,
                al => al.EntityId,
                l => l.Id,
                (al, l) => new { Log = al, Lead = l })
            .Join(_db.Stores,
                x => x.Lead.AssignedStoreId,
                s => s.Id,
                (x, s) => new DispatchHistoryItemDto(
                    x.Lead.Id,
                    x.Lead.LeadCode,
                    x.Lead.CustomerName,
                    x.Lead.CustomerPhone,
                    s.Id,
                    s.StoreName,
                    x.Log.Note,
                    x.Log.PerformedAt,
                    x.Lead.Status.ToString()))
            .ToListAsync(ct);

        return Result<List<DispatchHistoryItemDto>>.Success(dispatched);
    }
}
