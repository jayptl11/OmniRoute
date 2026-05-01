using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Dashboard.DTOs;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetSalesReport;

public record GetSalesReportQuery(
    string Period = "month",
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<SalesReportDto>;
