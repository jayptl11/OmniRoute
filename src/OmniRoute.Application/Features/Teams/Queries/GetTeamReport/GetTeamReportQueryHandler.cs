using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Teams.Queries.GetTeamReport;

internal sealed class GetTeamReportQueryHandler
    : IQueryHandler<GetTeamReportQuery, TeamReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetTeamReportQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TeamReportDto>> Handle(GetTeamReportQuery query, CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<TeamReportDto>.Failure(
                "INVALID_PERIOD", "Period phải là: week, month hoặc quarter.");

        var teamId = _currentUserService.TeamId;
        if (teamId is null)
            return Result<TeamReportDto>.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

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

        var teamMemberIds = await _db.Users
            .Where(u => u.TeamId == teamId)
            .Select(u => u.UserId)
            .ToListAsync(ct);

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l =>
                l.AssignedUserId.HasValue &&
                teamMemberIds.Contains(l.AssignedUserId.Value) &&
                l.AssignedAt >= periodStart &&
                l.AssignedAt <= periodEnd)
            .Select(l => new { l.Id, l.Status, l.SlaViolated, l.AssignedAt, l.CreatedAt })
            .ToListAsync(ct);

        var totalLeads = leads.Count;

        // ByStatus
        var byStatus = leads
            .GroupBy(l => l.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // SLA
        var slaViolatedCount = leads.Count(l => l.SlaViolated);
        var slaAchievedCount = totalLeads - slaViolatedCount;
        double? slaAchievedRate = totalLeads > 0
            ? Math.Round((double)slaAchievedCount / totalLeads * 100, 1)
            : null;

        // Win rate
        var processedStatuses = new HashSet<LeadStatus>
            { LeadStatus.Contacted, LeadStatus.InProgress, LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled };

        var totalProcessed = leads.Count(l => processedStatuses.Contains(l.Status));
        var wonCount = leads.Count(l => l.Status == LeadStatus.Won);

        double? winRate = totalProcessed > 0
            ? Math.Round((double)wonCount / totalProcessed * 100, 1)
            : null;

        // Daily trend
        var daySpan = (int)(periodEnd.Date - periodStart.Date).TotalDays + 1;
        var trendRaw = leads
            .Where(l => l.AssignedAt.HasValue)
            .GroupBy(l => l.AssignedAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyTrend = Enumerable.Range(0, daySpan)
            .Select(i => periodStart.Date.AddDays(i))
            .Select(d => new DailyLeadTrendDto(
                d.ToString("yyyy-MM-dd"),
                trendRaw.GetValueOrDefault(d, 0)))
            .ToList();

        return Result<TeamReportDto>.Success(new TeamReportDto(
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            TotalLeads: totalLeads,
            ByStatus: byStatus,
            SlaAchievedCount: slaAchievedCount,
            SlaViolatedCount: slaViolatedCount,
            SlaAchievedRate: slaAchievedRate,
            WonCount: wonCount,
            WinRate: winRate,
            DailyTrend: dailyTrend,
            GeneratedAt: now));
    }

    private static bool IsValidPeriod(string period) => period is "week" or "month" or "quarter";

    private static DateTime GetPeriodStart(string period, DateTime now) => period switch
    {
        "week"    => now.AddDays(-7),
        "month"   => now.AddMonths(-1),
        "quarter" => now.AddMonths(-3),
        _         => now.AddMonths(-1)
    };
}
