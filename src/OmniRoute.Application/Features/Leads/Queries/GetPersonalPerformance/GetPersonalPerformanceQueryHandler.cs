using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Leads.Queries.GetPersonalPerformance;

internal sealed class GetPersonalPerformanceQueryHandler
    : IQueryHandler<GetPersonalPerformanceQuery, PersonalPerformanceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetPersonalPerformanceQueryHandler(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PersonalPerformanceDto>> Handle(
        GetPersonalPerformanceQuery query,
        CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<PersonalPerformanceDto>.Failure(
                "INVALID_PERIOD", "Period phải là: week, month hoặc quarter.");

        var currentUserId = _currentUserService.GetUserId();
        var now = DateTime.UtcNow;
        var periodStart = GetPeriodStart(query.Period, now);

        // Lấy tất cả lead được gán trong kỳ
        var leads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.AssignedUserId == currentUserId && l.AssignedAt >= periodStart)
            .Select(l => new
            {
                l.Id,
                l.Status,
                l.SlaViolated,
                l.AssignedAt
            })
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

        // Thời gian phản hồi trung bình: từ AssignedAt → thời điểm đầu tiên STATUS_CHANGED → Contacted
        // Lấy các log STATUS_CHANGED→Contacted trong kỳ cho user hiện tại
        var leadIds = leads.Select(l => l.Id).ToList();

        double? avgResponseTimeMinutes = null;
        if (leadIds.Count > 0)
        {
            // Lấy log đầu tiên STATUS_CHANGED → Contacted cho từng lead
            var contactedLogs = await _db.ActivityLogs
                .AsNoTracking()
                .Where(al =>
                    al.EntityType == "LEAD" &&
                    leadIds.Contains(al.EntityId) &&
                    al.Action == "STATUS_CHANGED" &&
                    al.NewValue == "Contacted" &&
                    al.PerformedBy == currentUserId)
                .Select(al => new { al.EntityId, al.PerformedAt })
                .ToListAsync(ct);

            if (contactedLogs.Count > 0)
            {
                // Join với leads để tính delta
                var responseTimes = (
                    from log in contactedLogs
                    join lead in leads on log.EntityId equals lead.Id
                    where lead.AssignedAt.HasValue
                    select (log.PerformedAt - lead.AssignedAt!.Value).TotalMinutes
                ).ToList();

                if (responseTimes.Count > 0)
                    avgResponseTimeMinutes = Math.Round(responseTimes.Average(), 1);
            }
        }

        var dto = new PersonalPerformanceDto(
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: now,
            TotalAssigned: totalAssigned,
            TotalProcessed: totalProcessed,
            WonCount: wonCount,
            WinRate: winRate,
            AvgResponseTimeMinutes: avgResponseTimeMinutes,
            SlaViolatedCount: slaViolatedCount,
            GeneratedAt: now);

        return Result<PersonalPerformanceDto>.Success(dto);
    }

    private static bool IsValidPeriod(string period) =>
        period is "week" or "month" or "quarter";

    private static DateTime GetPeriodStart(string period, DateTime now) => period switch
    {
        "week"    => now.AddDays(-7),
        "month"   => now.AddMonths(-1),
        "quarter" => now.AddMonths(-3),
        _         => now.AddMonths(-1)
    };
}
