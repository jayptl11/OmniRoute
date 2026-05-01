using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Dashboard.DTOs;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetUnitComparison;

public record GetUnitComparisonQuery(
    string Period = "month",
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string SortBy = "leadCount") : IQuery<UnitComparisonDto>;
