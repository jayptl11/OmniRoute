using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StoreCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.StoreCode).IsUnique();

        builder.Property(x => x.StoreName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.MaxCapacity).HasDefaultValue(20);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasOne(x => x.Manager)
            .WithMany()
            .HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
