namespace OmniRoute.Application.Features.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string EntityType,
    Guid EntityId,
    bool IsRead,
    DateTime CreatedAt);

public record GetNotificationsResponse(
    List<NotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
