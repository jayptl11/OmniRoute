using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Audit.DTOs;

namespace OmniRoute.Application.Features.Audit.Queries.ExportAuditLogs;

internal sealed class ExportAuditLogsQueryHandler
    : IQueryHandler<ExportAuditLogsQuery, ExportAuditLogsResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IReportExportService _exportService;

    public ExportAuditLogsQueryHandler(IApplicationDbContext db, IReportExportService exportService)
    {
        _db = db;
        _exportService = exportService;
    }

    public async Task<Result<ExportAuditLogsResult>> Handle(
        ExportAuditLogsQuery query,
        CancellationToken ct)
    {
        var q = _db.ActivityLogs.AsNoTracking().AsQueryable();

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

        var items = await q
            .OrderByDescending(l => l.PerformedAt)
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

        var fileBytes = _exportService.ExportAuditToExcel(items);
        var fileName = $"AuditLog_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

        return Result<ExportAuditLogsResult>.Success(
            new ExportAuditLogsResult(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName));
    }
}
