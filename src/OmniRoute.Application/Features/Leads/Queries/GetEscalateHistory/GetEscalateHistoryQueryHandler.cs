using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetEscalateHistory;

internal sealed class GetEscalateHistoryQueryHandler
    : IQueryHandler<GetEscalateHistoryQuery, PagedResult<EscalateHistoryItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetEscalateHistoryQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<EscalateHistoryItemDto>>> Handle(
        GetEscalateHistoryQuery query,
        CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetUserId();

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Lấy các log escalate do TN hiện tại thực hiện trên LEAD
        var logsQuery = _db.ActivityLogs
            .AsNoTracking()
            .Where(al =>
                al.EntityType == "LEAD" &&
                al.Action == "ESCALATED" &&
                al.PerformedBy == currentUserId)
            .OrderByDescending(al => al.PerformedAt);

        var totalCount = await logsQuery.CountAsync(ct);

        var logs = await logsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (logs.Count == 0)
            return Result<PagedResult<EscalateHistoryItemDto>>.Success(
                new PagedResult<EscalateHistoryItemDto>([], totalCount, page, pageSize));

        // Lấy Lead details
        var leadIds = logs.Select(l => l.EntityId).Distinct().ToList();
        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => leadIds.Contains(l.Id))
            .Select(l => new { l.Id, l.LeadCode, l.CustomerName, l.CustomerPhone })
            .ToDictionaryAsync(l => l.Id, ct);

        // Lấy User details của EscalateTo (stored in NewValue as GUID string)
        var escalateToIds = logs
            .Where(l => l.NewValue != null && Guid.TryParse(l.NewValue, out _))
            .Select(l => Guid.Parse(l.NewValue!))
            .Distinct()
            .ToList();

        var escalateToUsers = await _db.Users
            .AsNoTracking()
            .Where(u => escalateToIds.Contains(u.UserId))
            .Select(u => new { u.UserId, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .ToDictionaryAsync(u => u.UserId, ct);

        var items = logs.Select(al =>
        {
            leads.TryGetValue(al.EntityId, out var lead);
            Guid? escalateTo = al.NewValue != null && Guid.TryParse(al.NewValue, out var parsed)
                ? parsed : null;
            string? escalateToName = escalateTo.HasValue && escalateToUsers.TryGetValue(escalateTo.Value, out var eu)
                ? eu.FullName : null;

            return new EscalateHistoryItemDto(
                LogId: al.Id,
                LeadId: al.EntityId,
                LeadCode: lead?.LeadCode ?? string.Empty,
                CustomerName: lead?.CustomerName ?? string.Empty,
                CustomerPhone: lead?.CustomerPhone ?? string.Empty,
                EscalateTo: escalateTo ?? Guid.Empty,
                EscalateToName: escalateToName,
                Reason: al.Note,
                PerformedAt: al.PerformedAt);
        }).ToList();

        return Result<PagedResult<EscalateHistoryItemDto>>.Success(
            new PagedResult<EscalateHistoryItemDto>(items, totalCount, page, pageSize));
    }
}
