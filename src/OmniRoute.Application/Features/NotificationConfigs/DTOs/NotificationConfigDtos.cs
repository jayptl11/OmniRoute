namespace OmniRoute.Application.Features.NotificationConfigs.DTOs;

public record NotificationConfigDto(
    Guid Id,
    string NotificationType,
    string TargetRole,
    bool IsEnabled,
    DateTime UpdatedAt);
