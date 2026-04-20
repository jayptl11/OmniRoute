using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class MasterDataItemConfiguration : IEntityTypeConfiguration<MasterDataItem>
{
    public void Configure(EntityTypeBuilder<MasterDataItem> builder)
    {
        builder.ToTable("MasterDataItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => new { x.Category, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.Category, x.IsActive });
    }
}
