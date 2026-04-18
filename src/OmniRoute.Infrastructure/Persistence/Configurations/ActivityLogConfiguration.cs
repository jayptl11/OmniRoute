using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
        builder.Property(x => x.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NewValue).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.PerformedAt);

        builder.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
