using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TeamName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TeamType)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasOne(x => x.Leader)
            .WithMany()
            .HasForeignKey(x => x.LeaderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
