using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Dashboard.DTOs;

namespace OmniRoute.Application.Features.Dashboard.Queries.GetDrillDown;

/// <summary>
/// BQL-02: Drill-down dashboard.
/// level = "unit" (by store) | "channel" (by channel).
/// id = storeId (Guid as string) for level=unit, or channel name for level=channel.
/// </summary>
public record GetDrillDownQuery(
    string Level,
    string? Id = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<DrillDownDto>;
