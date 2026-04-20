namespace OmniRoute.Application.Features.Teams.DTOs;

public record TeamDto(
    Guid Id,
    string TeamName,
    string TeamType,
    Guid? LeaderId,
    Guid? StoreId,
    bool IsActive,
    DateTime CreatedAt);
