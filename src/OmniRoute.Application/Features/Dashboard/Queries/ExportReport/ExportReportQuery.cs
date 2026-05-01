using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Dashboard.Queries.ExportReport;

/// <summary>BQL-06: Xuất báo cáo theo kỳ. reportType: overview | unitComparison | sales.</summary>
public record ExportReportQuery(
    string ReportType,
    string Period = "month",
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<ExportReportResult>;

public record ExportReportResult(byte[] FileBytes, string ContentType, string FileName);
