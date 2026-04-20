using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;

namespace OmniRoute.Application.Features.Users.Commands.AdminSendResetLink;

internal sealed class AdminSendResetLinkCommandHandler : ICommandHandler<AdminSendResetLinkCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public AdminSendResetLinkCommandHandler(
        IApplicationDbContext db,
        ITokenService tokenService,
        IEmailService emailService)
    {
        _db = db;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<Result> Handle(AdminSendResetLinkCommand command, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.");

        if (!user.IsActive)
            return Result.Failure("USER_INACTIVE", "Cannot send reset link to a deactivated account.");

        var resetToken = _tokenService.GenerateResetPasswordToken(user.Email);

        // In production, the frontend URL comes from config. We emit the token to the email service.
        var message = $"Admin đã gửi yêu cầu đặt lại mật khẩu cho tài khoản của bạn.<br/>" +
                      $"Token đặt lại mật khẩu (hết hạn sau một thời gian ngắn):<br/>" +
                      $"<strong>{resetToken}</strong><br/><br/>" +
                      $"Sử dụng token này tại màn hình đặt lại mật khẩu để thiết lập mật khẩu mới.";

        await _emailService.SendNotificationEmailAsync(
            user.Email,
            "Yêu cầu đặt lại mật khẩu từ Admin",
            message,
            ct);

        return Result.Success();
    }
}
