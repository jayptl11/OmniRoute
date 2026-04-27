using Microsoft.EntityFrameworkCore;
using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Users.DTOs;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Application.Features.Users.Commands.CreateUser;

internal sealed class CreateUserCommandHandler
    : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordService _passwordService;

    public CreateUserCommandHandler(
        IApplicationDbContext db,
        IPasswordService passwordService)
    {
        _db = db;
        _passwordService = passwordService;
    }

    public async Task<Result<CreateUserResponse>> Handle(
        CreateUserCommand command,
        CancellationToken ct)
    {
        var emailExists = await _db.Users.AnyAsync(u => u.Email == command.Email, ct);
        if (emailExists)
            return Result<CreateUserResponse>.Failure("EMAIL_TAKEN", "Email is already in use.");

        var usernameExists = await _db.Users.AnyAsync(u => u.Username == command.Username, ct);
        if (usernameExists)
            return Result<CreateUserResponse>.Failure("USERNAME_TAKEN", "Username is already in use.");

        var roleExists = await _db.Roles.AnyAsync(r => r.RoleId == command.RoleId, ct);
        if (!roleExists)
            return Result<CreateUserResponse>.Failure("ROLE_NOT_FOUND", "The specified role does not exist.");

        var passwordHash = _passwordService.HashPassword(command.Password);

        var user = User.Create(
            Guid.NewGuid(),
            command.Email,
            command.Username,
            passwordHash,
            command.FirstName,
            command.LastName,
            command.RoleId);

        if (command.StoreId.HasValue)
            user.AssignToStore(command.StoreId.Value);

        var profile = new UserProfile
        {
            ProfileId = Guid.NewGuid(),
            UserId = user.UserId,
            Phone = command.Phone
        };

        _db.Users.Add(user);
        _db.UserProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);

        return Result<CreateUserResponse>.Success(
            new CreateUserResponse(user.UserId, user.Username, user.Email));
    }

}
