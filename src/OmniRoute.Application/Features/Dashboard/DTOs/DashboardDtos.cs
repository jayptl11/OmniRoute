namespace OmniRoute.Application.Features.Dashboard.DTOs;

// BQL-01: Dashboard tổng hợp toàn hệ thống
public record DashboardOverviewDto(
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    KpiCardsDto KpiCards,
    Dictionary<string, int> LeadsByChannel,
    Dictionary<string, int> LeadsByNeedType,
    List<DailyTrendItemDto> DailyTrend,
    List<TopStoreItemDto> Top5Stores,
    DateTime GeneratedAt);

public record KpiCardsDto(
    int TotalLeadsToday,
    int TotalLeadsThisWeek,
    int TotalLeadsThisMonth,
    double? SlaAchievedRate,
    double? WinRate,
    int SlaViolatedCount);

public record DailyTrendItemDto(string Date, int TotalLeads);

public record TopStoreItemDto(Guid StoreId, string StoreName, int LeadCount);

// BQL-02: Drill-down dashboard
public record DrillDownDto(
    string Level,
    string? EntityId,
    string? EntityName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalLeads,
    Dictionary<string, int> ByStatus,
    List<DrillDownChildDto> Children);

public record DrillDownChildDto(string Label, int Count);

// BQL-03: Theo dõi KPI phân luồng
public record RoutingKpiDto(
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    double RuleMatchRate,
    double? AvgTimeToAssignMinutes,
    double? SlaAchievedRate,
    double? EscalationRate,
    RoutingKpiComparisonDto? Comparison,
    List<StoreSlAItemDto> SlaByStore,
    DateTime GeneratedAt);

public record RoutingKpiComparisonDto(
    double? PrevRuleMatchRate,
    double? PrevAvgTimeToAssignMinutes,
    double? PrevSlaAchievedRate,
    double? PrevEscalationRate,
    DateTime PrevPeriodStart,
    DateTime PrevPeriodEnd);

public record StoreSlAItemDto(Guid StoreId, string StoreName, double SlaAchievedRate, int TotalLeads);

// BQL-04: So sánh hiệu suất giữa các đơn vị
public record UnitComparisonDto(
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    List<UnitComparisonItemDto> Items,
    DateTime GeneratedAt);

public record UnitComparisonItemDto(
    Guid StoreId,
    string StoreName,
    string? Region,
    int LeadCount,
    double? WinRate,
    double? SlaAchievedRate,
    double? AvgProcessingTimeHours);

// BQL-05: Báo cáo hiệu quả bán hàng
public record SalesReportDto(
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalLeads,
    int ContactedCount,
    int WonCount,
    double? ContactRate,
    double? WinRate,
    Dictionary<string, int> WonByChannel,
    Dictionary<string, int> WonByNeedType,
    List<DailySalesTrendItemDto> DailyTrend,
    DateTime GeneratedAt);

public record DailySalesTrendItemDto(string Date, int TotalLeads, int WonCount);
