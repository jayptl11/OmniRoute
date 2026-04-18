using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;

namespace OmniRoute.Application.Features.Users.Commands.UpdateUser;

internal sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IApplicationDbContext _db;

    public UpdateUserCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == command.UserId, ct);
        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.");

        var emailTaken = await _db.Users
            .AnyAsync(u => u.Email == command.Email && u.UserId != command.UserId, ct);
        if (emailTaken)
            return Result.Failure("EMAIL_TAKEN", "Email is already in use by another user.");

        var roleExists = await _db.Roles.AnyAsync(r => r.RoleId == command.RoleId, ct);
        if (!roleExists)
            return Result.Failure("ROLE_NOT_FOUND", "The specified role does not exist.");

        user.UpdateDetails(command.FirstName, command.LastName, command.Email);
        user.AssignRole(command.RoleId);
        user.AssignToStore(command.StoreId);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
