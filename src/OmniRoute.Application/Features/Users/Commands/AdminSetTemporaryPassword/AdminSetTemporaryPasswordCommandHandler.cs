using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;

namespace OmniRoute.Application.Features.Users.Commands.AdminSetTemporaryPassword;

internal sealed class AdminSetTemporaryPasswordCommandHandler : ICommandHandler<AdminSetTemporaryPasswordCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordService _passwordService;

    public AdminSetTemporaryPasswordCommandHandler(
        IApplicationDbContext db,
        IPasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    public async Task<Result> Handle(AdminSetTemporaryPasswordCommand command, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.");

        var hash = _passwordService.HashPassword(command.TemporaryPassword);
        user.SetTemporaryPassword(hash);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
