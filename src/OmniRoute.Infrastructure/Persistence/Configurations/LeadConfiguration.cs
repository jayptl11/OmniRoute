using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LeadCode).IsRequired().HasMaxLength(30);
        builder.HasIndex(x => x.LeadCode).IsUnique();

        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CustomerPhone).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.CustomerPhone);

        builder.Property(x => x.CustomerAddress).HasMaxLength(500);
        builder.Property(x => x.CustomerEmail).HasMaxLength(200);

        builder.Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.NeedType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.NeedDescription).IsRequired();

        // product_interest stored as JSON string
        builder.Property(x => x.ProductInterest).HasColumnType("nvarchar(max)");

        builder.Property(x => x.PriorityLevel)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.RoutingType)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.AssignedGroup)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.SlaViolated).HasDefaultValue(false);

        // FK: created_by → User (no cascade — prevent multiple cascade paths)
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // FK: assigned_user_id → User (no cascade)
        builder.HasOne(x => x.AssignedUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // FK: assigned_store_id → Store
        builder.HasOne(x => x.AssignedStore)
            .WithMany(s => s.Leads)
            .HasForeignKey(x => x.AssignedStoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
