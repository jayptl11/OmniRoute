using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TicketCode).IsRequired().HasMaxLength(30);
        builder.HasIndex(x => x.TicketCode).IsUnique();

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

        builder.Property(x => x.PriorityLevel)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.SlaViolated).HasDefaultValue(false);

        builder.Property(x => x.EscalatedReason).HasMaxLength(1000);
        builder.Property(x => x.SatisfactionNote).HasMaxLength(1000);

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

        // FK: assigned_store_id → Store (no cascade)
        builder.HasOne(x => x.AssignedStore)
            .WithMany()
            .HasForeignKey(x => x.AssignedStoreId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
