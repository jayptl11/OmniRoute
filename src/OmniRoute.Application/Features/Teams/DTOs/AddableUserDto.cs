namespace OmniRoute.Application.Features.Teams.DTOs;

public record AddableUserDto(
    Guid UserId,
    string FullName,
    string Username,
    string? RoleName,
    string? RoleDisplayName,
    bool HasTeam);
