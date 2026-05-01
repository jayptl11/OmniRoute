using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.SystemStats.DTOs;

namespace OmniRoute.Application.Features.SystemStats.Queries.GetSystemStats;

public record GetSystemStatsQuery(
    string Period = "month",
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<SystemStatsDto>;
