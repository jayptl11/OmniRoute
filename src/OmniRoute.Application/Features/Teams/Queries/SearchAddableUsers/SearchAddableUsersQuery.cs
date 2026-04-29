using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Teams.DTOs;

namespace OmniRoute.Application.Features.Teams.Queries.SearchAddableUsers;

public record SearchAddableUsersQuery(string? Search) : IQuery<List<AddableUserDto>>;
