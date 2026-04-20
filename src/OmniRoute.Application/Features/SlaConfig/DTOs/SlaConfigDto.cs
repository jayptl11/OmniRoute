namespace OmniRoute.Application.Features.SlaConfig.DTOs;

public record SlaConfigDto(
    Guid Id,
    string AssignedGroup,
    string PriorityLevel,
    int MaxHours,
    int WarningBeforeHours,
    bool IsActive);
