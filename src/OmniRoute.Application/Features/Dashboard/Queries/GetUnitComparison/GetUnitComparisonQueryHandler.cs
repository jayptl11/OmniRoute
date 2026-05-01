using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Dashboard.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetUnitComparison;

internal sealed class GetUnitComparisonQueryHandler
    : IQueryHandler<GetUnitComparisonQuery, UnitComparisonDto>
{
    private readonly IApplicationDbContext _db;

    public GetUnitComparisonQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UnitComparisonDto>> Handle(
        GetUnitComparisonQuery query,
        CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<UnitComparisonDto>.Failure(
                "INVALID_PERIOD", "Period phải là: week, month hoặc quarter.");

        var now = DateTime.UtcNow;
        DateTime periodStart;
        DateTime periodEnd = now;

        if (query.DateFrom.HasValue && query.DateTo.HasValue)
        {
            periodStart = query.DateFrom.Value;
            periodEnd = query.DateTo.Value;
        }
        else
        {
            periodStart = GetPeriodStart(query.Period, now);
        }

        var stores = await _db.Stores
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.StoreName, s.Region })
            .ToListAsync(ct);

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedAt >= periodStart && l.AssignedAt <= periodEnd
                        && l.AssignedStoreId.HasValue)
            .Select(l => new
            {
                l.AssignedStoreId,
                l.Status,
                l.SlaViolated,
                l.AssignedAt,
                l.ClosedAt
            })
            .ToListAsync(ct);

        var processedStatuses = new HashSet<LeadStatus>
            { LeadStatus.Contacted, LeadStatus.InProgress, LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled };

        var items = stores.Select(store =>
        {
            var storeLeads = leads.Where(l => l.AssignedStoreId == store.Id).ToList();
            var total = storeLeads.Count;

            var processed = storeLeads.Count(l => processedStatuses.Contains(l.Status));
            var won = storeLeads.Count(l => l.Status == LeadStatus.Won);
            double? winRate = processed > 0 ? Math.Round((double)won / processed * 100, 1) : null;

            var violated = storeLeads.Count(l => l.SlaViolated);
            double? slaAchievedRate = total > 0
                ? Math.Round((double)(total - violated) / total * 100, 1)
                : null;

            var closedLeads = storeLeads
                .Where(l => l.ClosedAt.HasValue && l.AssignedAt.HasValue)
                .ToList();

            double? avgProcessingHours = closedLeads.Count > 0
                ? Math.Round(closedLeads.Average(l =>
                    (l.ClosedAt!.Value - l.AssignedAt!.Value).TotalHours), 1)
                : null;

            return new UnitComparisonItemDto(
                store.Id,
                store.StoreName,
                store.Region,
                total,
                winRate,
                slaAchievedRate,
                avgProcessingHours);
        }).ToList();

        // Sort
        items = query.SortBy.ToLower() switch
        {
            "winrate"              => items.OrderByDescending(x => x.WinRate).ToList(),
            "slachievedrate"       => items.OrderByDescending(x => x.SlaAchievedRate).ToList(),
            "avgprocessingtime"    => items.OrderBy(x => x.AvgProcessingTimeHours).ToList(),
            _                      => items.OrderByDescending(x => x.LeadCount).ToList()
        };

        return Result<UnitComparisonDto>.Success(new UnitComparisonDto(
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            Items: items,
            GeneratedAt: now));
    }

    private static bool IsValidPeriod(string period) => period is "week" or "month" or "quarter";

    private static DateTime GetPeriodStart(string period, DateTime now) => period switch
    {
        "week"    => now.AddDays(-7),
        "quarter" => now.AddMonths(-3),
        _         => now.AddMonths(-1)
    };
}
