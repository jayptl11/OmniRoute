namespace OmniRoute.Application.Features.Teams.DTOs;

public record TeamMemberDto(
    Guid UserId,
    string FullName,
    string? RoleName,
    string? RoleDisplayName,
    bool IsActive,
    int CurrentWorkload,
    DateTime? LastAssignedAt);
