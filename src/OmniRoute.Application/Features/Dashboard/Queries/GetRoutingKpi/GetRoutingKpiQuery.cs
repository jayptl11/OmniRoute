using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Dashboard.DTOs;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetRoutingKpi;

public record GetRoutingKpiQuery(
    string Period = "month",
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<RoutingKpiDto>;
