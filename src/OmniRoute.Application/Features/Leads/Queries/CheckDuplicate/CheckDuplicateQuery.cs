using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.CheckDuplicate;

public record CheckDuplicateQuery(string Phone) : IQuery<DuplicateCheckDto>;
