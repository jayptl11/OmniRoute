using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Users.DTOs;
using OmniRoute.Domain.Entities;

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
        return await _db.Roles
            .Select(r => new RoleDto(r.RoleId, r.RoleName))
            .ToListAsync(ct);
    }
}
