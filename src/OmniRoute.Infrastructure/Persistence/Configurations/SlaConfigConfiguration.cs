using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class SlaConfigConfiguration : IEntityTypeConfiguration<SlaConfig>
{
    public void Configure(EntityTypeBuilder<SlaConfig> builder)
    {
        builder.ToTable("SlaConfigs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssignedGroup)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.PriorityLevel)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.HasIndex(x => new { x.AssignedGroup, x.PriorityLevel }).IsUnique();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Seed default SLA values per spec §QT-11
        builder.HasData(
            // SALE
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000001"), AssignedGroup = AssignedGroup.Sale, PriorityLevel = PriorityLevel.High, MaxHours = 2, WarningBeforeHours = 1, IsActive = true },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000002"), AssignedGroup = AssignedGroup.Sale, PriorityLevel = PriorityLevel.Medium, MaxHours = 4, WarningBeforeHours = 1, IsActive = true },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000003"), AssignedGroup = AssignedGroup.Sale, PriorityLevel = PriorityLevel.Low, MaxHours = 8, WarningBeforeHours = 2, IsActive = true },
            // CSKH
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000004"), AssignedGroup = AssignedGroup.Cskh, PriorityLevel = PriorityLevel.High, MaxHours = 1, WarningBeforeHours = 1, IsActive = true },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000005"), AssignedGroup = AssignedGroup.Cskh, PriorityLevel = PriorityLevel.Medium, MaxHours = 4, WarningBeforeHours = 1, IsActive = true },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000006"), AssignedGroup = AssignedGroup.Cskh, PriorityLevel = PriorityLevel.Low, MaxHours = 24, WarningBeforeHours = 4, IsActive = true },
            // STORE_SUPPORT
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000007"), AssignedGroup = AssignedGroup.StoreSupport, PriorityLevel = PriorityLevel.High, MaxHours = 4, WarningBeforeHours = 1, IsActive = true },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000008"), AssignedGroup = AssignedGroup.StoreSupport, PriorityLevel = PriorityLevel.Medium, MaxHours = 8, WarningBeforeHours = 2, IsActive = true },
            new { Id = Guid.Parse("a1000001-0000-0000-0000-000000000009"), AssignedGroup = AssignedGroup.StoreSupport, PriorityLevel = PriorityLevel.Low, MaxHours = 24, WarningBeforeHours = 4, IsActive = true }
        );
    }
}
