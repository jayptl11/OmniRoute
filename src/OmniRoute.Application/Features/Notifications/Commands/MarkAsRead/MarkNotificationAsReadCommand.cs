using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Notifications.Commands.MarkAsRead;

public record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;
