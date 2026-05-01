using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Dashboard.DTOs;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetDrillDown;

internal sealed class GetDrillDownQueryHandler
    : IQueryHandler<GetDrillDownQuery, DrillDownDto>
{
    private readonly IApplicationDbContext _db;

    public GetDrillDownQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DrillDownDto>> Handle(
        GetDrillDownQuery query,
        CancellationToken ct)
    {
        if (query.Level is not ("unit" or "channel"))
            return Result<DrillDownDto>.Failure(
                "INVALID_LEVEL", "Level phải là: unit hoặc channel.");

        var now = DateTime.UtcNow;
        var dateFrom = query.DateFrom ?? now.AddMonths(-1);
        var dateTo = query.DateTo ?? now;

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.CreatedAt >= dateFrom && l.CreatedAt <= dateTo)
            .Select(l => new
            {
                l.Id,
                l.Status,
                l.Channel,
                l.AssignedStoreId
            })
            .ToListAsync(ct);

        string? entityName = null;
        List<DrillDownChildDto> children;
        int total;

        if (query.Level == "unit")
        {
            if (!string.IsNullOrWhiteSpace(query.Id) && Guid.TryParse(query.Id, out var storeId))
            {
                // Drill into specific store
                var store = await _db.Stores
                    .AsNoTracking()
                    .Where(s => s.Id == storeId)
                    .Select(s => new { s.StoreName })
                    .FirstOrDefaultAsync(ct);

                entityName = store?.StoreName;

                var storeLeads = leads.Where(l => l.AssignedStoreId == storeId).ToList();
                total = storeLeads.Count;

                children = storeLeads
                    .GroupBy(l => l.Status.ToString())
                    .Select(g => new DrillDownChildDto(g.Key, g.Count()))
                    .OrderByDescending(x => x.Count)
                    .ToList();
            }
            else
            {
                // Top-level: breakdown by store
                var storeIds = leads
                    .Where(l => l.AssignedStoreId.HasValue)
                    .Select(l => l.AssignedStoreId!.Value)
                    .Distinct()
                    .ToList();

                var storeNames = await _db.Stores
                    .AsNoTracking()
                    .Where(s => storeIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.StoreName })
                    .ToListAsync(ct);

                var storeNameMap = storeNames.ToDictionary(s => s.Id, s => s.StoreName);
                total = leads.Count;

                children = leads
                    .Where(l => l.AssignedStoreId.HasValue)
                    .GroupBy(l => l.AssignedStoreId!.Value)
                    .Select(g => new DrillDownChildDto(
                        storeNameMap.GetValueOrDefault(g.Key, g.Key.ToString()),
                        g.Count()))
                    .OrderByDescending(x => x.Count)
                    .ToList();
            }
        }
        else // channel
        {
            if (!string.IsNullOrWhiteSpace(query.Id))
            {
                // Drill into specific channel
                entityName = query.Id;
                var channelLeads = leads
                    .Where(l => l.Channel.ToString().Equals(query.Id, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                total = channelLeads.Count;

                children = channelLeads
                    .GroupBy(l => l.Status.ToString())
                    .Select(g => new DrillDownChildDto(g.Key, g.Count()))
                    .OrderByDescending(x => x.Count)
                    .ToList();
            }
            else
            {
                // Top-level: breakdown by channel
                total = leads.Count;
                children = leads
                    .GroupBy(l => l.Channel.ToString())
                    .Select(g => new DrillDownChildDto(g.Key, g.Count()))
                    .OrderByDescending(x => x.Count)
                    .ToList();
            }
        }

        var byStatus = leads
            .GroupBy(l => l.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return Result<DrillDownDto>.Success(new DrillDownDto(
            Level: query.Level,
            EntityId: query.Id,
            EntityName: entityName,
            PeriodStart: dateFrom,
            PeriodEnd: dateTo,
            TotalLeads: total,
            ByStatus: byStatus,
            Children: children));
    }
}
