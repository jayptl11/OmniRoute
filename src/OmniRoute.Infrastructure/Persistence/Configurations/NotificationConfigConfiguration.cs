using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfigConfiguration : IEntityTypeConfiguration<NotificationConfig>
{
    public void Configure(EntityTypeBuilder<NotificationConfig> builder)
    {
        builder.ToTable("NotificationConfigs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NotificationType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TargetRole).IsRequired().HasMaxLength(10);
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => new { x.NotificationType, x.TargetRole }).IsUnique();

        // Seed defaults per spec §11:
        // NEW_LEAD       → assigned user (handled in code) — also alert TN
        // SLA_WARNING    → assigned user (in code) + TN
        // SLA_VIOLATED   → assigned user (in code) + TN + QL
        // ESCALATED      → TN
        // REASSIGNED     → new assignee (in code)
        // FOLLOW_UP_DUE  → assigned user (in code)
        builder.HasData(
            Build("c1000001-0000-0000-0000-000000000001", "NEW_LEAD",      "TN",  true),
            Build("c1000002-0000-0000-0000-000000000002", "SLA_WARNING",   "TN",  true),
            Build("c1000003-0000-0000-0000-000000000003", "SLA_VIOLATED",  "TN",  true),
            Build("c1000004-0000-0000-0000-000000000004", "SLA_VIOLATED",  "QL",  true),
            Build("c1000005-0000-0000-0000-000000000005", "ESCALATED",     "TN",  true),
            Build("c1000006-0000-0000-0000-000000000006", "ESCALATED",     "QL",  true)
        );
    }

    private static object Build(string id, string type, string role, bool enabled) =>
        new
        {
            Id = Guid.Parse(id),
            NotificationType = type,
            TargetRole = role,
            IsEnabled = enabled,
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
}
