using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Tickets.DTOs;

namespace OmniRoute.Application.Features.Tickets.Queries.GetTicketPerformance;

public record GetTicketPerformanceQuery(string Period) : IQuery<TicketPerformanceDto>;
