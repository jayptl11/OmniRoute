using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Auth.DTOs;
using OmniRoute.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace OmniRoute.Application.Features.Auth.Commands.Login;

internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var identifier = request.Identifier.Trim();

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == identifier || u.Username == identifier, cancellationToken);

        if (user == null)
            return Result<LoginResponse>.Failure("INVALID_CREDENTIALS", "Invalid username/email or password");

        if (!user.IsActive)
            return Result<LoginResponse>.Failure("ACCOUNT_LOCKED", "Your account has been locked. Please contact support.");

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Failure("INVALID_CREDENTIALS", "Invalid username/email or password");

        var now = DateTime.UtcNow;
        user.UpdateLastLogin(now);

        var accessToken = _tokenService.GenerateAccessToken(user);

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            TokenId = NewId.NextGuid(),
            UserId = user.UserId,
            Token = _tokenService.HashRefreshToken(refreshTokenValue),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_tokenService.RefreshTokenExpirationDays)
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
            user.RoleId,
            user.Role?.RoleName
        ));
    }
}

