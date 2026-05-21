using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Auth.DTOs;
using OmniRoute.Domain.Constants;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Application.Features.Auth.Commands.LoginWithGoogle;

internal sealed class LoginWithGoogleCommandHandler : ICommandHandler<LoginWithGoogleCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleAuthService _googleAuth;
    private readonly ITokenService _tokenService;

    public LoginWithGoogleCommandHandler(
        IApplicationDbContext context,
        IGoogleAuthService googleAuth,
        ITokenService tokenService)
    {
        _context = context;
        _googleAuth = googleAuth;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponse>> Handle(LoginWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var googleUser = await _googleAuth.ValidateIdTokenAsync(request.IdToken, cancellationToken);
        if (googleUser == null)
        {
            return Result<LoginResponse>.Failure("INVALID_GOOGLE_TOKEN", "Invalid Google token");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == googleUser.Email, cancellationToken);

        Guid? resolvedRoleId = null;
        string? resolvedRoleName = null;

        if (user == null)
        {
            var username = googleUser.Email.Split('@')[0];
            var finalUsername = await EnsureUniqueUsernameAsync(username, cancellationToken);

            var defaultRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == RoleCatalog.Consultant, cancellationToken);

            resolvedRoleId = defaultRole?.RoleId;
            resolvedRoleName = defaultRole?.RoleName;

            user = User.Create(
                NewId.NextGuid(),
                googleUser.Email,
                finalUsername,
                string.Empty,
                googleUser.GivenName,
                googleUser.FamilyName,
                resolvedRoleId
            );

            _context.Users.Add(user);

            var userProfile = new UserProfile
            {
                ProfileId = NewId.NextGuid(),
                UserId = user.UserId
            };
            user.UserProfile = userProfile;
            _context.UserProfiles.Add(userProfile);

            _context.SetAuditUserId(user.UserId);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            resolvedRoleId = user.RoleId;
            resolvedRoleName = user.Role?.RoleName;
        }

        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure("ACCOUNT_LOCKED", "Your account has been locked. Please contact support.");
        }

        user.UpdateLastLogin(DateTime.UtcNow);

        var accessToken = _tokenService.GenerateAccessToken(user);

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            TokenId = NewId.NextGuid(),
            UserId = user.UserId,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays)
        });

        _context.SetAuditUserId(user.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken,
            refreshTokenValue,
            user.UserId,
            user.Email,
            user.Username,
            user.LastLogin,
            resolvedRoleId,
            resolvedRoleName,
            RoleCatalog.GetDisplayName(resolvedRoleName)
        ));
    }

    private async Task<string> EnsureUniqueUsernameAsync(string baseUsername, CancellationToken cancellationToken)
    {
        var candidate = baseUsername;
        var suffix = 0;

        while (await _context.Users.AsNoTracking().AnyAsync(u => u.Username == candidate, cancellationToken))
        {
            suffix++;
            candidate = $"{baseUsername}{suffix}";
        }

        return candidate;
    }
}
