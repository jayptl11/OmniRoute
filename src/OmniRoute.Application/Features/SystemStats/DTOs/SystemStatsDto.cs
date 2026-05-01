namespace OmniRoute.Application.Features.SystemStats.DTOs;

public record SystemStatsDto(
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalLeadsProcessed,
    double AutoRoutingSuccessRate,
    int DefaultGroupHits,
    int TotalErrors,
    List<DailyLeadStatsDto> DailyTrend,
    Dictionary<string, int> LeadsByGroup,
    DateTime GeneratedAt);

public record DailyLeadStatsDto(
    string Date,
    int TotalLeads,
    int AutoRouted,
    int DefaultGroupHits);
