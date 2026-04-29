using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Teams.DTOs;

public record TeamReportDto(
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalLeads,
    Dictionary<string, int> ByStatus,
    int SlaAchievedCount,
    int SlaViolatedCount,
    double? SlaAchievedRate,
    int WonCount,
    double? WinRate,
    List<DailyLeadTrendDto> DailyTrend,
    DateTime GeneratedAt);
