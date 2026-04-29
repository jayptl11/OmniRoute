using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.DTOs;

/// <summary>QL-04 — Báo cáo hiệu quả đơn vị theo kỳ.</summary>
public record StoreReportDto(
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
