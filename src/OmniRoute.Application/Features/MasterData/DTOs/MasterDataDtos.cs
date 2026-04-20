namespace OmniRoute.Application.Features.MasterData.DTOs;

public record MasterDataItemDto(
    Guid Id,
    string Category,
    string Code,
    string DisplayName,
    string? Description,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt);

public record EnumListItemDto(string Value, string DisplayName);
