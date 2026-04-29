using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Leads.Queries.GetTeamLeadOverview;

internal sealed class GetTeamLeadOverviewQueryHandler
    : IQueryHandler<GetTeamLeadOverviewQuery, TeamLeadOverviewDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetTeamLeadOverviewQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TeamLeadOverviewDto>> Handle(
        GetTeamLeadOverviewQuery query,
        CancellationToken ct)
    {
        var teamId = _currentUserService.TeamId;

        if (teamId is null)
            return Result<TeamLeadOverviewDto>.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        var teamMemberIds = await _db.Users
            .Where(u => u.TeamId == teamId)
            .Select(u => u.UserId)
            .ToListAsync(ct);

        var maxWarningHours = await _db.SlaConfigs
            .Where(s => s.IsActive)
            .MaxAsync(s => (int?)s.WarningBeforeHours, ct) ?? 4;

        var now = DateTime.UtcNow;
        var warningThreshold = now.AddHours(maxWarningHours);
        var sevenDaysAgo = now.AddDays(-6).Date;

        var baseQuery = _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedUserId.HasValue && teamMemberIds.Contains(l.AssignedUserId.Value));

        // TN-01 aggregate counts
        var pendingResponse = await baseQuery
            .CountAsync(l => l.Status == LeadStatus.Assigned, ct);

        var inProgress = await baseQuery
            .CountAsync(l => l.Status == LeadStatus.InProgress, ct);

        var slaViolated = await baseQuery
            .CountAsync(l => l.SlaViolated, ct);

        var slaNearDeadline = await baseQuery
            .CountAsync(l => !l.SlaViolated
                && l.SlaDeadline != null
                && l.SlaDeadline <= warningThreshold, ct);

        // Trend: leads created per day over last 7 days
        var trendRaw = await baseQuery
            .Where(l => l.CreatedAt >= sevenDaysAgo)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var trendByDay = trendRaw.ToDictionary(t => t.Date, t => t.Count);

        var trend = Enumerable.Range(0, 7)
            .Select(i => now.AddDays(-6 + i).Date)
            .Select(d => new DailyLeadTrendDto(
                d.ToString("yyyy-MM-dd"),
                trendByDay.GetValueOrDefault(d, 0)))
            .ToList();

        return Result<TeamLeadOverviewDto>.Success(new TeamLeadOverviewDto(
            pendingResponse,
            inProgress,
            slaViolated,
            slaNearDeadline,
            trend));
    }
}
