namespace OmniRoute.Application.Features.StoreManagement.DTOs;

public record AddableStoreUserDto(
    Guid UserId,
    string FullName,
    string Username,
    string? RoleName,
    bool HasStore);
