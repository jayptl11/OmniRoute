using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace OmniRoute.Application.Features.Auth.Commands.ResetPassword;

internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IOTPService _otpService;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IOTPService otpService)
    {
        _context = context;
        _tokenService = tokenService;
        _otpService = otpService;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = _tokenService.ValidateResetPasswordToken(request.ResetToken);

        if (email == null)
            return Result.Failure("INVALID_TOKEN", "Invalid or expired reset token");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
            return Result.Failure("USER_NOT_FOUND", "User not found.");

        user.UpdatePassword(_otpService.HashPassword(request.NewPassword));

        _context.SetAuditUserId(user.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

