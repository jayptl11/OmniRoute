namespace OmniRoute.Application.Common.DTOs;

public record UserPickerOptionDto(
    Guid UserId,
    string FullName,
    string? RoleName,
    string? RoleDisplayName);
