using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Dashboard.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetSalesReport;

internal sealed class GetSalesReportQueryHandler
    : IQueryHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetSalesReportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SalesReportDto>> Handle(
        GetSalesReportQuery query,
        CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<SalesReportDto>.Failure(
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

        // Only sale leads (AssignedGroup = Sale)
        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l =>
                l.AssignedGroup == AssignedGroup.Sale &&
                l.AssignedAt >= periodStart && l.AssignedAt <= periodEnd)
            .Select(l => new
            {
                l.Id,
                l.Status,
                l.Channel,
                l.NeedType,
                l.AssignedAt
            })
            .ToListAsync(ct);

        var totalLeads = leads.Count;

        var contactedStatuses = new HashSet<LeadStatus>
            { LeadStatus.Contacted, LeadStatus.InProgress, LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled };
        var contactedCount = leads.Count(l => contactedStatuses.Contains(l.Status));
        var wonCount = leads.Count(l => l.Status == LeadStatus.Won);

        double? contactRate = totalLeads > 0
            ? Math.Round((double)contactedCount / totalLeads * 100, 1)
            : null;

        double? winRate = contactedCount > 0
            ? Math.Round((double)wonCount / contactedCount * 100, 1)
            : null;

        // Won by channel
        var wonByChannel = leads
            .Where(l => l.Status == LeadStatus.Won)
            .GroupBy(l => l.Channel.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Won by need type
        var wonByNeedType = leads
            .Where(l => l.Status == LeadStatus.Won && l.NeedType.HasValue)
            .GroupBy(l => l.NeedType!.Value.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Daily trend
        var daySpan = (int)(periodEnd.Date - periodStart.Date).TotalDays + 1;
        var byDate = leads
            .Where(l => l.AssignedAt.HasValue)
            .GroupBy(l => l.AssignedAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dailyTrend = Enumerable.Range(0, daySpan)
            .Select(i => periodStart.Date.AddDays(i))
            .Select(d =>
            {
                var dayLeads = byDate.GetValueOrDefault(d, []);
                return new DailySalesTrendItemDto(
                    d.ToString("yyyy-MM-dd"),
                    dayLeads.Count,
                    dayLeads.Count(l => l.Status == LeadStatus.Won));
            })
            .ToList();

        return Result<SalesReportDto>.Success(new SalesReportDto(
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            TotalLeads: totalLeads,
            ContactedCount: contactedCount,
            WonCount: wonCount,
            ContactRate: contactRate,
            WinRate: winRate,
            WonByChannel: wonByChannel,
            WonByNeedType: wonByNeedType,
            DailyTrend: dailyTrend,
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
