using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreReport;

public record GetStoreReportQuery(
    string Period = "month",
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<StoreReportDto>;
