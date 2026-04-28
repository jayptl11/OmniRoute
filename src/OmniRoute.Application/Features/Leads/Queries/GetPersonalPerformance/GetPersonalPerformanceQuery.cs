using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetPersonalPerformance;

/// <param name="Period">"week" | "month" | "quarter"</param>
public record GetPersonalPerformanceQuery(string Period) : IQuery<PersonalPerformanceDto>;
