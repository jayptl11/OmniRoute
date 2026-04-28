using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class FollowUpTaskConfiguration : IEntityTypeConfiguration<FollowUpTask>
{
    public void Configure(EntityTypeBuilder<FollowUpTask> builder)
    {
        builder.ToTable("FollowUpTasks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IsCompleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.UserId, x.IsCompleted });
        builder.HasIndex(x => x.DueAt);

        builder.HasOne(x => x.Lead)
            .WithMany()
            .HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
