namespace OmniRoute.Domain.Entities;

/// <summary>
/// QT-12: Configures which roles receive which notification types.
/// Allows admin to toggle delivery per notification type / role pair.
/// </summary>
public class NotificationConfig
{
    public Guid Id { get; private set; }

    /// <summary>SLA_WARNING | SLA_VIOLATED | NEW_LEAD | ESCALATED | REASSIGNED | FOLLOW_UP_DUE</summary>
    public string NotificationType { get; private set; } = string.Empty;

    /// <summary>Role name (TV, SA, CS, DP, TN, QL, QT, BQL) that receives this notification type.</summary>
    public string TargetRole { get; private set; } = string.Empty;

    public bool IsEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private NotificationConfig() { } // EF Core

    public static NotificationConfig Create(string notificationType, string targetRole, bool isEnabled = true)
    {
        return new NotificationConfig
        {
            Id = Guid.NewGuid(),
            NotificationType = notificationType,
            TargetRole = targetRole,
            IsEnabled = isEnabled,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
