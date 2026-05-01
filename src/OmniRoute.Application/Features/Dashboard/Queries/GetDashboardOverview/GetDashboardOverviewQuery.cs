using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Dashboard.DTOs;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetDashboardOverview;

public record GetDashboardOverviewQuery(
    string Period = "month",
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<DashboardOverviewDto>;
