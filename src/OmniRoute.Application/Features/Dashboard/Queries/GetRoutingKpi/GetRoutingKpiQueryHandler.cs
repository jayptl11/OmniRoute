using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Dashboard.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetRoutingKpi;

internal sealed class GetRoutingKpiQueryHandler
    : IQueryHandler<GetRoutingKpiQuery, RoutingKpiDto>
{
    private readonly IApplicationDbContext _db;

    public GetRoutingKpiQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<RoutingKpiDto>> Handle(
        GetRoutingKpiQuery query,
        CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<RoutingKpiDto>.Failure(
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

        var currentMetrics = await ComputeMetricsAsync(periodStart, periodEnd, ct);

        // Comparison: previous period of the same length
        var periodLength = periodEnd - periodStart;
        var prevPeriodEnd = periodStart;
        var prevPeriodStart = prevPeriodEnd - periodLength;
        var prevMetrics = await ComputeMetricsAsync(prevPeriodStart, prevPeriodEnd, ct);

        var comparison = new RoutingKpiComparisonDto(
            prevMetrics.RuleMatchRate,
            prevMetrics.AvgTimeToAssignMinutes,
            prevMetrics.SlaAchievedRate,
            prevMetrics.EscalationRate,
            prevPeriodStart,
            prevPeriodEnd);

        // SLA by store
        var leadsWithStore = await _db.Leads
            .AsNoTracking()
            .Where(l => l.CreatedAt >= periodStart && l.CreatedAt <= periodEnd
                        && l.AssignedStoreId.HasValue)
            .Select(l => new { l.AssignedStoreId, l.SlaViolated })
            .ToListAsync(ct);

        var storeIds = leadsWithStore.Select(l => l.AssignedStoreId!.Value).Distinct().ToList();
        var stores = await _db.Stores
            .AsNoTracking()
            .Where(s => storeIds.Contains(s.Id))
            .Select(s => new { s.Id, s.StoreName })
            .ToListAsync(ct);

        var storeNameMap = stores.ToDictionary(s => s.Id, s => s.StoreName);

        var slaByStore = leadsWithStore
            .GroupBy(l => l.AssignedStoreId!.Value)
            .Select(g =>
            {
                var total = g.Count();
                var violated = g.Count(x => x.SlaViolated);
                var achieved = total - violated;
                double rate = total > 0 ? Math.Round((double)achieved / total * 100, 1) : 0;
                return new StoreSlAItemDto(
                    g.Key,
                    storeNameMap.GetValueOrDefault(g.Key, "Unknown"),
                    rate,
                    total);
            })
            .OrderByDescending(x => x.TotalLeads)
            .ToList();

        return Result<RoutingKpiDto>.Success(new RoutingKpiDto(
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            RuleMatchRate: currentMetrics.RuleMatchRate,
            AvgTimeToAssignMinutes: currentMetrics.AvgTimeToAssignMinutes,
            SlaAchievedRate: currentMetrics.SlaAchievedRate,
            EscalationRate: currentMetrics.EscalationRate,
            Comparison: comparison,
            SlaByStore: slaByStore,
            GeneratedAt: now));
    }

    private async Task<(double RuleMatchRate, double? AvgTimeToAssignMinutes, double? SlaAchievedRate, double? EscalationRate)>
        ComputeMetricsAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.CreatedAt >= start && l.CreatedAt <= end)
            .Select(l => new
            {
                l.Id,
                l.Status,
                l.AssignedGroup,
                l.SlaViolated,
                l.CreatedAt,
                l.AssignedAt
            })
            .ToListAsync(ct);

        var total = leads.Count;
        if (total == 0)
            return (0, null, null, null);

        // Rule match = leads with an assigned group (not defaulted to null)
        var ruleMatched = leads.Count(l => l.AssignedGroup.HasValue);
        double ruleMatchRate = Math.Round((double)ruleMatched / total * 100, 1);

        // Avg time to assign
        var assignedLeads = leads.Where(l => l.AssignedAt.HasValue).ToList();
        double? avgTimeToAssign = assignedLeads.Count > 0
            ? Math.Round(assignedLeads.Average(l => (l.AssignedAt!.Value - l.CreatedAt).TotalMinutes), 1)
            : null;

        // SLA achieved
        var slaViolated = leads.Count(l => l.SlaViolated);
        double? slaAchievedRate = Math.Round((double)(total - slaViolated) / total * 100, 1);

        // Escalation rate: count leads with Escalated status from ActivityLog
        var escalatedCount = await _db.ActivityLogs
            .AsNoTracking()
            .CountAsync(a =>
                a.EntityType == "LEAD" &&
                a.Action == "ESCALATED" &&
                a.PerformedAt >= start &&
                a.PerformedAt <= end, ct);

        double? escalationRate = total > 0
            ? Math.Round((double)escalatedCount / total * 100, 1)
            : null;

        return (ruleMatchRate, avgTimeToAssign, slaAchievedRate, escalationRate);
    }

    private static bool IsValidPeriod(string period) => period is "week" or "month" or "quarter";

    private static DateTime GetPeriodStart(string period, DateTime now) => period switch
    {
        "week"    => now.AddDays(-7),
        "quarter" => now.AddMonths(-3),
        _         => now.AddMonths(-1)
    };
}
