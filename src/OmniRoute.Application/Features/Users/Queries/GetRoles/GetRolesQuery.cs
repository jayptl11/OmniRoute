using MediatR;
using OmniRoute.Application.Features.Users.DTOs;
using OmniRoute.Application.Common.Models;

namespace OmniRoute.Application.Features.Users.Queries.GetRoles;

public record GetRolesQuery : IRequest<List<RoleDto>>;
