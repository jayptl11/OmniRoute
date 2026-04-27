namespace OmniRoute.Application.Features.Users.DTOs;

public record UserListItemDto(
    Guid UserId,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    string? RoleName,
    Guid? RoleId,
    Guid? StoreId,
    bool IsActive,
    DateTime? LastLogin,
    DateTime CreatedAt);

public record CreateUserResponse(
    Guid UserId,
    string Username,
    string Email);

public record ToggleUserStatusResponse(
    Guid UserId,
    bool IsActive,
    int ActiveLeadCount);
