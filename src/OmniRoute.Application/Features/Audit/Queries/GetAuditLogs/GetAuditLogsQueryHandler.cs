using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Audit.DTOs;

namespace OmniRoute.Application.Features.Audit.Queries.GetAuditLogs;

internal sealed class GetAuditLogsQueryHandler
    : IQueryHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAuditLogsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> Handle(
        GetAuditLogsQuery query,
        CancellationToken ct)
    {
        var q = _db.ActivityLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(l => l.EntityType == query.EntityType.ToUpper());

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(l => l.Action.Contains(query.Action));

        if (query.PerformedBy.HasValue)
            q = q.Where(l => l.PerformedBy == query.PerformedBy.Value);

        if (query.DateFrom.HasValue)
            q = q.Where(l => l.PerformedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(l => l.PerformedAt <= query.DateTo.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(l => l.PerformedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new AuditLogDto(
                l.Id,
                l.EntityType,
                l.EntityId,
                l.Action,
                l.OldValue,
                l.NewValue,
                l.Note,
                l.PerformedBy,
                l.PerformedByUser != null
                    ? l.PerformedByUser.FirstName + " " + l.PerformedByUser.LastName
                    : null,
                l.IsInternal,
                l.PerformedAt))
            .ToListAsync(ct);

        return Result<PagedResult<AuditLogDto>>.Success(
            new PagedResult<AuditLogDto>(items, totalCount, query.Page, query.PageSize));
    }
}
