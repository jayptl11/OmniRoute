using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class RoutingRuleConfiguration : IEntityTypeConfiguration<RoutingRule>
{
    public void Configure(EntityTypeBuilder<RoutingRule> builder)
    {
        builder.ToTable("RoutingRules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RuleName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.PriorityOrder).IsUnique();

        builder.Property(x => x.ConditionChannelJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ConditionKeywordsJson).HasColumnType("nvarchar(max)");

        builder.Property(x => x.ActionGroup)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasOne(x => x.ActionTeam)
            .WithMany()
            .HasForeignKey(x => x.ActionTeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
