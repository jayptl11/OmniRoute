using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Dashboard.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetDashboardOverview;

internal sealed class GetDashboardOverviewQueryHandler
    : IQueryHandler<GetDashboardOverviewQuery, DashboardOverviewDto>
{
    private readonly IApplicationDbContext _db;

    public GetDashboardOverviewQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DashboardOverviewDto>> Handle(
        GetDashboardOverviewQuery query,
        CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<DashboardOverviewDto>.Failure(
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
                l.Channel,
                l.NeedType,
                l.SlaViolated,
                l.AssignedStoreId,
                l.CreatedAt
            })
            .ToListAsync(ct);

        var totalLeads = leads.Count;

        // KPI cards
        var todayStart = now.Date;
        var weekStart = now.AddDays(-7).Date;
        var monthStart = now.AddMonths(-1).Date;

        var totalLeadsToday = leads.Count(l => l.CreatedAt.Date >= todayStart);
        var totalLeadsThisWeek = leads.Count(l => l.CreatedAt.Date >= weekStart);
        var totalLeadsThisMonth = leads.Count(l => l.CreatedAt.Date >= monthStart);

        var slaViolatedCount = leads.Count(l => l.SlaViolated);
        double? slaAchievedRate = totalLeads > 0
            ? Math.Round((double)(totalLeads - slaViolatedCount) / totalLeads * 100, 1)
            : null;

        var processedStatuses = new HashSet<LeadStatus>
            { LeadStatus.Contacted, LeadStatus.InProgress, LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled };
        var totalProcessed = leads.Count(l => processedStatuses.Contains(l.Status));
        var wonCount = leads.Count(l => l.Status == LeadStatus.Won);
        double? winRate = totalProcessed > 0
            ? Math.Round((double)wonCount / totalProcessed * 100, 1)
            : null;

        // By channel
        var leadsByChannel = leads
            .GroupBy(l => l.Channel.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // By need type
        var leadsByNeedType = leads
            .Where(l => l.NeedType.HasValue)
            .GroupBy(l => l.NeedType!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Daily trend (30 days max)
        var trendDays = Math.Min((int)(periodEnd.Date - periodStart.Date).TotalDays + 1, 30);
        var trendStart = periodEnd.Date.AddDays(-(trendDays - 1));
        var trendByDate = leads
            .Where(l => l.CreatedAt.Date >= trendStart)
            .GroupBy(l => l.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyTrend = Enumerable.Range(0, trendDays)
            .Select(i => trendStart.AddDays(i))
            .Select(d => new DailyTrendItemDto(
                d.ToString("yyyy-MM-dd"),
                trendByDate.GetValueOrDefault(d, 0)))
            .ToList();

        // Top 5 stores
        var storeIds = leads
            .Where(l => l.AssignedStoreId.HasValue)
            .GroupBy(l => l.AssignedStoreId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        var stores = await _db.Stores
            .AsNoTracking()
            .Where(s => storeIds.Contains(s.Id))
            .Select(s => new { s.Id, s.StoreName })
            .ToListAsync(ct);

        var storeMap = stores.ToDictionary(s => s.Id, s => s.StoreName);

        var top5Stores = leads
            .Where(l => l.AssignedStoreId.HasValue)
            .GroupBy(l => l.AssignedStoreId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopStoreItemDto(
                g.Key,
                storeMap.GetValueOrDefault(g.Key, "Unknown"),
                g.Count()))
            .ToList();

        var kpiCards = new KpiCardsDto(
            totalLeadsToday,
            totalLeadsThisWeek,
            totalLeadsThisMonth,
            slaAchievedRate,
            winRate,
            slaViolatedCount);

        return Result<DashboardOverviewDto>.Success(new DashboardOverviewDto(
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            KpiCards: kpiCards,
            LeadsByChannel: leadsByChannel,
            LeadsByNeedType: leadsByNeedType,
            DailyTrend: dailyTrend,
            Top5Stores: top5Stores,
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
