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
    private readonly IEmailService _emailService;

    public CreateUserCommandHandler(
        IApplicationDbContext db,
        IPasswordService passwordService,
        IEmailService emailService)
    {
        _db = db;
        _passwordService = passwordService;
        _emailService = emailService;
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

        var tempPassword = GenerateTempPassword();
        var passwordHash = _passwordService.HashPassword(tempPassword);

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

        await _emailService.SendWelcomeEmailAsync(command.Email, command.Username, tempPassword, ct);

        return Result<CreateUserResponse>.Success(
            new CreateUserResponse(user.UserId, user.Username, user.Email, tempPassword));
    }

    private static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string all = upper + lower + digits;

        var rng = new Random();
        var chars = new char[10];
        chars[0] = upper[rng.Next(upper.Length)];
        chars[1] = digits[rng.Next(digits.Length)];
        for (int i = 2; i < chars.Length; i++)
            chars[i] = all[rng.Next(all.Length)];

        return new string(chars.OrderBy(_ => rng.Next()).ToArray());
    }
}
