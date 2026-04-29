using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Teams.Queries.GetMemberPerformance;

internal sealed class GetMemberPerformanceQueryHandler
    : IQueryHandler<GetMemberPerformanceQuery, MemberPerformanceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetMemberPerformanceQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<MemberPerformanceDto>> Handle(
        GetMemberPerformanceQuery query,
        CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<MemberPerformanceDto>.Failure(
                "INVALID_PERIOD", "Period phải là: week, month hoặc quarter.");

        var teamId = _currentUserService.TeamId;
        if (teamId is null)
            return Result<MemberPerformanceDto>.Failure("NO_TEAM", "Bạn chưa được gán vào đội nào.");

        // Kiểm tra UserId thuộc team TN
        var targetUser = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == query.UserId && u.TeamId == teamId)
            .Select(u => new { u.UserId, FullName = (u.FirstName + " " + u.LastName).Trim() })
            .FirstOrDefaultAsync(ct);

        if (targetUser is null)
            return Result<MemberPerformanceDto>.Failure(
                "MEMBER_NOT_FOUND", "Thành viên không tồn tại hoặc không thuộc đội của bạn.");

        var now = DateTime.UtcNow;
        var periodStart = GetPeriodStart(query.Period, now);

        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedUserId == query.UserId && l.AssignedAt >= periodStart)
            .Select(l => new { l.Id, l.Status, l.SlaViolated, l.AssignedAt })
            .ToListAsync(ct);

        var totalAssigned = leads.Count;

        var processedStatuses = new HashSet<LeadStatus>
        {
            LeadStatus.Contacted, LeadStatus.InProgress, LeadStatus.Won, LeadStatus.Lost, LeadStatus.Cancelled
        };

        var totalProcessed = leads.Count(l => processedStatuses.Contains(l.Status));
        var wonCount = leads.Count(l => l.Status == LeadStatus.Won);
        var slaViolatedCount = leads.Count(l => l.SlaViolated);

        double? winRate = totalProcessed > 0
            ? Math.Round((double)wonCount / totalProcessed * 100, 1)
            : null;

        var leadIds = leads.Select(l => l.Id).ToList();
        double? avgResponseTimeMinutes = null;

        if (leadIds.Count > 0)
        {
            var contactedLogs = await _db.ActivityLogs
                .AsNoTracking()
                .Where(al =>
                    al.EntityType == "LEAD" &&
                    leadIds.Contains(al.EntityId) &&
                    al.Action == "STATUS_CHANGED" &&
                    al.NewValue == "Contacted" &&
                    al.PerformedBy == query.UserId)
                .Select(al => new { al.EntityId, al.PerformedAt })
                .ToListAsync(ct);

            if (contactedLogs.Count > 0)
            {
                // Lấy log đầu tiên cho mỗi lead
                var firstContactedPerLead = contactedLogs
                    .GroupBy(l => l.EntityId)
                    .Select(g => g.OrderBy(l => l.PerformedAt).First())
                    .ToList();

                var leadAssignedTimes = leads
                    .Where(l => l.AssignedAt.HasValue)
                    .ToDictionary(l => l.Id, l => l.AssignedAt!.Value);

                var responseTimes = firstContactedPerLead
                    .Where(cl => leadAssignedTimes.ContainsKey(cl.EntityId))
                    .Select(cl => (cl.PerformedAt - leadAssignedTimes[cl.EntityId]).TotalMinutes)
                    .Where(t => t >= 0)
                    .ToList();

                if (responseTimes.Count > 0)
                    avgResponseTimeMinutes = Math.Round(responseTimes.Average(), 1);
            }
        }

        return Result<MemberPerformanceDto>.Success(new MemberPerformanceDto(
            UserId: targetUser.UserId,
            FullName: targetUser.FullName,
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: now,
            TotalAssigned: totalAssigned,
            TotalProcessed: totalProcessed,
            WonCount: wonCount,
            WinRate: winRate,
            AvgResponseTimeMinutes: avgResponseTimeMinutes,
            SlaViolatedCount: slaViolatedCount,
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
