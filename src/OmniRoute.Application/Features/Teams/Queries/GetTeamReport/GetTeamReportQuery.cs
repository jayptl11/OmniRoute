using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Teams.DTOs;

namespace OmniRoute.Application.Features.Teams.Queries.GetTeamReport;

public record GetTeamReportQuery(string Period = "month", DateTime? DateFrom = null, DateTime? DateTo = null)
    : IQuery<TeamReportDto>;
