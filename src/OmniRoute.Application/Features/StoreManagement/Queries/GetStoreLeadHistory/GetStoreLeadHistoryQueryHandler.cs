using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreLeadHistory;

internal sealed class GetStoreLeadHistoryQueryHandler
    : IQueryHandler<GetStoreLeadHistoryQuery, PagedResult<StoreLeadHistoryItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetStoreLeadHistoryQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<StoreLeadHistoryItemDto>>> Handle(
        GetStoreLeadHistoryQuery query,
        CancellationToken ct)
    {
        var storeId = _currentUserService.StoreId;

        if (storeId is null)
            return Result<PagedResult<StoreLeadHistoryItemDto>>.Failure("NO_STORE", "Bạn chưa được gán vào đơn vị nào.");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Lấy tất cả lead IDs thuộc store này
        var storeLeadIds = await _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedStoreId == storeId)
            .Select(l => l.Id)
            .ToListAsync(ct);

        if (storeLeadIds.Count == 0)
            return Result<PagedResult<StoreLeadHistoryItemDto>>.Success(
                new PagedResult<StoreLeadHistoryItemDto>([], 0, page, pageSize));

        var logsQuery = _db.ActivityLogs
            .AsNoTracking()
            .Where(al => al.EntityType == "LEAD" && storeLeadIds.Contains(al.EntityId));

        if (query.UserId.HasValue)
            logsQuery = logsQuery.Where(al => al.PerformedBy == query.UserId.Value);

        if (query.DateFrom.HasValue)
            logsQuery = logsQuery.Where(al => al.PerformedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            logsQuery = logsQuery.Where(al => al.PerformedAt <= query.DateTo.Value);

        var totalCount = await logsQuery.CountAsync(ct);

        var logs = await logsQuery
            .OrderByDescending(al => al.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (logs.Count == 0)
            return Result<PagedResult<StoreLeadHistoryItemDto>>.Success(
                new PagedResult<StoreLeadHistoryItemDto>([], totalCount, page, pageSize));

        // Lookup lead details
        var leadIds = logs.Select(l => l.EntityId).Distinct().ToList();
        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => leadIds.Contains(l.Id))
            .Select(l => new { l.Id, l.LeadCode, l.CustomerName, l.CustomerPhone })
            .ToDictionaryAsync(l => l.Id, ct);

        // Lookup performer names
        var performerIds = logs
            .Where(l => l.PerformedBy.HasValue)
            .Select(l => l.PerformedBy!.Value)
            .Distinct()
            .ToList();

        var performers = await _db.Users
            .AsNoTracking()
            .Where(u => performerIds.Contains(u.UserId))
            .Select(u => new { u.UserId, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(u => u.UserId, ct);

        var items = logs.Select(al =>
        {
            leads.TryGetValue(al.EntityId, out var lead);
            string? performerName = al.PerformedBy.HasValue && performers.TryGetValue(al.PerformedBy.Value, out var p)
                ? p.FullName : null;

            return new StoreLeadHistoryItemDto(
                LogId: al.Id,
                LeadId: al.EntityId,
                LeadCode: lead?.LeadCode,
                CustomerName: lead?.CustomerName,
                CustomerPhone: lead?.CustomerPhone,
                Action: al.Action,
                OldValue: al.OldValue,
                NewValue: al.NewValue,
                Note: al.Note,
                PerformedBy: al.PerformedBy,
                PerformedByName: performerName,
                PerformedAt: al.PerformedAt);
        }).ToList();

        return Result<PagedResult<StoreLeadHistoryItemDto>>.Success(
            new PagedResult<StoreLeadHistoryItemDto>(items, totalCount, page, pageSize));
    }
}
