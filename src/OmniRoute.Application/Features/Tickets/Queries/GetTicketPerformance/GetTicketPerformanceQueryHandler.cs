using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.DTOs;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Tickets.Queries.GetTicketPerformance;

internal sealed class GetTicketPerformanceQueryHandler
    : IQueryHandler<GetTicketPerformanceQuery, TicketPerformanceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetTicketPerformanceQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<TicketPerformanceDto>> Handle(
        GetTicketPerformanceQuery query,
        CancellationToken ct)
    {
        if (!IsValidPeriod(query.Period))
            return Result<TicketPerformanceDto>.Failure(
                "INVALID_PERIOD", "Period phải là: week, month hoặc quarter.");

        var currentUserId = _currentUserService.GetUserId();
        var now = DateTime.UtcNow;
        var periodStart = GetPeriodStart(query.Period, now);

        // Lấy tất cả ticket được gán trong kỳ
        var tickets = await _db.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedUserId == currentUserId && t.AssignedAt >= periodStart)
            .Select(t => new
            {
                t.Id,
                t.Status,
                t.SlaViolated,
                t.AssignedAt,
                t.ClosedAt,
                t.SatisfactionScore
            })
            .ToListAsync(ct);

        var totalAssigned = tickets.Count;

        var processedStatuses = new HashSet<TicketStatus>
        {
            TicketStatus.InProgress,
            TicketStatus.WaitingCustomer,
            TicketStatus.Escalated,
            TicketStatus.Resolved,
            TicketStatus.Closed
        };

        var totalProcessed = tickets.Count(t => processedStatuses.Contains(t.Status));
        var resolvedCount = tickets.Count(t => t.Status is TicketStatus.Resolved or TicketStatus.Closed);
        var slaViolatedCount = tickets.Count(t => t.SlaViolated);

        // Tỷ lệ đúng hạn: ticket đã xử lý mà không vi phạm SLA / tổng đã xử lý
        double? onTimeRate = totalProcessed > 0
            ? Math.Round((double)(totalProcessed - slaViolatedCount) / totalProcessed * 100, 1)
            : null;

        // Thời gian xử lý trung bình: từ AssignedAt → ClosedAt (chỉ ticket đã Closed/Resolved)
        var closedTickets = tickets
            .Where(t => t.Status is TicketStatus.Resolved or TicketStatus.Closed
                        && t.AssignedAt.HasValue
                        && t.ClosedAt.HasValue)
            .Select(t => (t.ClosedAt!.Value - t.AssignedAt!.Value).TotalMinutes)
            .ToList();

        double? avgHandlingTimeMinutes = closedTickets.Count > 0
            ? Math.Round(closedTickets.Average(), 1)
            : null;

        // Điểm hài lòng trung bình
        var satisfactionScores = tickets
            .Where(t => t.SatisfactionScore.HasValue)
            .Select(t => (double)t.SatisfactionScore!.Value)
            .ToList();

        double? avgSatisfactionScore = satisfactionScores.Count > 0
            ? Math.Round(satisfactionScores.Average(), 2)
            : null;

        var dto = new TicketPerformanceDto(
            Period: query.Period,
            PeriodStart: periodStart,
            PeriodEnd: now,
            TotalAssigned: totalAssigned,
            TotalProcessed: totalProcessed,
            ResolvedCount: resolvedCount,
            OnTimeRate: onTimeRate,
            AvgHandlingTimeMinutes: avgHandlingTimeMinutes,
            AvgSatisfactionScore: avgSatisfactionScore,
            SlaViolatedCount: slaViolatedCount,
            GeneratedAt: now);

        return Result<TicketPerformanceDto>.Success(dto);
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
