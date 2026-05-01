using MediatR;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Dashboard.Queries.GetDashboardOverview;
using OmniRoute.Application.Features.Dashboard.Queries.GetSalesReport;
using OmniRoute.Application.Features.Dashboard.Queries.GetUnitComparison;

namespace OmniRoute.Application.Features.Dashboard.Queries.ExportReport;

internal sealed class ExportReportQueryHandler
    : IQueryHandler<ExportReportQuery, ExportReportResult>
{
    private readonly ISender _sender;
    private readonly IReportExportService _exportService;

    public ExportReportQueryHandler(ISender sender, IReportExportService exportService)
    {
        _sender = sender;
        _exportService = exportService;
    }

    public async Task<Result<ExportReportResult>> Handle(
        ExportReportQuery query,
        CancellationToken ct)
    {
        if (query.ReportType is not ("overview" or "unitComparison" or "sales"))
            return Result<ExportReportResult>.Failure(
                "INVALID_REPORT_TYPE", "reportType phải là: overview, unitComparison hoặc sales.");

        if (query.Format is not ("excel" or "pdf"))
            return Result<ExportReportResult>.Failure(
                "INVALID_FORMAT", "format phải là: excel hoặc pdf.");

        object data;
        string reportLabel;

        switch (query.ReportType)
        {
            case "overview":
            {
                var result = await _sender.Send(
                    new GetDashboardOverviewQuery(query.Period, query.DateFrom, query.DateTo), ct);
                if (!result.IsSuccess)
                    return Result<ExportReportResult>.Failure(result.ErrorCode, result.ErrorMessage);
                data = result.Value!;
                reportLabel = "DashboardOverview";
                break;
            }
            case "unitComparison":
            {
                var result = await _sender.Send(
                    new GetUnitComparisonQuery(query.Period, query.DateFrom, query.DateTo), ct);
                if (!result.IsSuccess)
                    return Result<ExportReportResult>.Failure(result.ErrorCode, result.ErrorMessage);
                data = result.Value!;
                reportLabel = "UnitComparison";
                break;
            }
            default: // "sales"
            {
                var result = await _sender.Send(
                    new GetSalesReportQuery(query.Period, query.DateFrom, query.DateTo), ct);
                if (!result.IsSuccess)
                    return Result<ExportReportResult>.Failure(result.ErrorCode, result.ErrorMessage);
                data = result.Value!;
                reportLabel = "SalesReport";
                break;
            }
        }

        if (query.Format == "pdf")
        {
            var pdfBytes = _exportService.ExportToPdf(query.ReportType, data);
            var pdfFileName = $"{reportLabel}_{query.Period}_{DateTime.UtcNow:yyyyMMdd}.pdf";
            return Result<ExportReportResult>.Success(
                new ExportReportResult(pdfBytes, "application/pdf", pdfFileName));
        }

        var fileBytes = _exportService.ExportToExcel(query.ReportType, data);
        var fileName = $"{reportLabel}_{query.Period}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

        return Result<ExportReportResult>.Success(
            new ExportReportResult(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName));
    }
}
