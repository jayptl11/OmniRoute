using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniRoute.Domain.Entities;

namespace OmniRoute.Infrastructure.Persistence.Configurations;

internal sealed class AiApiKeyConfiguration : IEntityTypeConfiguration<AiApiKey>
{
    public void Configure(EntityTypeBuilder<AiApiKey> builder)
    {
        builder.ToTable("AiApiKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(k => k.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(k => k.EncryptedKey)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(k => k.Priority)
            .IsRequired();

        builder.Property(k => k.IsActive)
            .IsRequired();

        builder.Property(k => k.FailureCount)
            .IsRequired();

        builder.Property(k => k.LastFailedAt);
        builder.Property(k => k.LastUsedAt);

        builder.Property(k => k.CreatedAt)
            .IsRequired();

        builder.Property(k => k.UpdatedAt)
            .IsRequired();

        builder.HasIndex(k => new { k.Provider, k.Priority });
        builder.HasIndex(k => k.IsActive);
    }
}
