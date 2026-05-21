using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Users.DTOs;
using OmniRoute.Domain.Constants;

namespace OmniRoute.Application.Features.Users.Queries.GetRoles;

internal sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly IApplicationDbContext _db;

    public GetRolesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var roles = await _db.Roles
            .OrderBy(r => r.RoleName)
            .ToListAsync(ct);

        return roles
            .Select(r => new RoleDto(
                r.RoleId,
                r.RoleName,
                RoleCatalog.GetDisplayName(r.RoleName) ?? r.RoleName))
            .ToList();
    }
}
