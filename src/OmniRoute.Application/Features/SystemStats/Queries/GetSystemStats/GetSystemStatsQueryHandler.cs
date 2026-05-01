using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.SystemStats.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.SystemStats.Queries.GetSystemStats;

internal sealed class GetSystemStatsQueryHandler
    : IQueryHandler<GetSystemStatsQuery, SystemStatsDto>
{
    private readonly IApplicationDbContext _db;

    public GetSystemStatsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SystemStatsDto>> Handle(
        GetSystemStatsQuery query,
        CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<SystemStatsDto>.Failure(
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

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.CreatedAt >= periodStart && l.CreatedAt <= periodEnd)
            .Select(l => new
            {
                l.Id,
                l.Status,
                l.AssignedGroup,
                l.RoutingType,
                l.CreatedAt
            })
            .ToListAsync(ct);

        var totalLeads = leads.Count;

        // Auto-routing = leads that were routed by engine (not pending dispatch/assignment)
        var autoRouted = leads.Count(l =>
            l.Status != LeadStatus.PendingDispatch &&
            l.Status != LeadStatus.PendingAssignment);

        var defaultGroupHits = leads.Count(l =>
            l.AssignedGroup == null);

        double autoRoutingSuccessRate = totalLeads > 0
            ? Math.Round((double)autoRouted / totalLeads * 100, 1)
            : 0;

        // By group
        var leadsByGroup = leads
            .Where(l => l.AssignedGroup.HasValue)
            .GroupBy(l => l.AssignedGroup!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Daily trend
        var daySpan = (int)(periodEnd.Date - periodStart.Date).TotalDays + 1;
        var byDate = leads
            .GroupBy(l => l.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dailyTrend = Enumerable.Range(0, daySpan)
            .Select(i => periodStart.Date.AddDays(i))
            .Select(d =>
            {
                var dayLeads = byDate.GetValueOrDefault(d, []);
                var dayAutoRouted = dayLeads.Count(l =>
                    l.Status != LeadStatus.PendingDispatch &&
                    l.Status != LeadStatus.PendingAssignment);
                var dayDefaults = dayLeads.Count(l => l.AssignedGroup == null);
                return new DailyLeadStatsDto(
                    d.ToString("yyyy-MM-dd"),
                    dayLeads.Count,
                    dayAutoRouted,
                    dayDefaults);
            })
            .ToList();

        return Result<SystemStatsDto>.Success(new SystemStatsDto(
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            TotalLeadsProcessed: totalLeads,
            AutoRoutingSuccessRate: autoRoutingSuccessRate,
            DefaultGroupHits: defaultGroupHits,
            TotalErrors: 0,
            DailyTrend: dailyTrend,
            LeadsByGroup: leadsByGroup,
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
