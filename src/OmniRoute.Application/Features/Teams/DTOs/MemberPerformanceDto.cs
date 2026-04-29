namespace OmniRoute.Application.Features.Teams.DTOs;

public record MemberPerformanceDto(
    Guid UserId,
    string FullName,
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalAssigned,
    int TotalProcessed,
    int WonCount,
    double? WinRate,
    double? AvgResponseTimeMinutes,
    int SlaViolatedCount,
    DateTime GeneratedAt);
