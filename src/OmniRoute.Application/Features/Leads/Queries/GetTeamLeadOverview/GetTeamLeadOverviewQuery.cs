using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetTeamLeadOverview;

public record GetTeamLeadOverviewQuery : IQuery<TeamLeadOverviewDto>;
