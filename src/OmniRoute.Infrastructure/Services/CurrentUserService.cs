using OmniRoute.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace OmniRoute.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Role =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    public Guid? TeamId
    {
        get
        {
            var val = _httpContextAccessor.HttpContext?.User.FindFirst("teamId")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public Guid? StoreId
    {
        get
        {
            var val = _httpContextAccessor.HttpContext?.User.FindFirst("storeId")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public Guid GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated");

        return userId;
    }

    public Guid? TryGetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

