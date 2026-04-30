using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.NotificationConfigs.Commands.UpdateNotificationConfig;

public record UpdateNotificationConfigCommand(Guid Id, bool IsEnabled) : ICommand;
