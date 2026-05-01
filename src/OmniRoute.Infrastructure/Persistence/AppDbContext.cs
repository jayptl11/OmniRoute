using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace OmniRoute.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    // Default role for new registrations (lowest privilege)
    public static readonly Guid TvRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Guid? _manualUserId;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TokenBlacklist> TokenBlacklist => Set<TokenBlacklist>();

    // Lead management
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<RoutingRule> RoutingRules => Set<RoutingRule>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<SlaConfig> SlaConfigs => Set<SlaConfig>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MasterDataItem> MasterDataItems => Set<MasterDataItem>();
    public DbSet<FollowUpTask> FollowUpTasks => Set<FollowUpTask>();
    public DbSet<NotificationConfig> NotificationConfigs => Set<NotificationConfig>();
    public DbSet<AiApiKey> AiApiKeys => Set<AiApiKey>();

    public void SetAuditUserId(Guid userId)
    {
        _manualUserId = userId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.RoleId);
            entity.Property(x => x.RoleName).IsRequired();

            entity.HasData(
                new Role { RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), RoleName = "TV" },
                new Role { RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), RoleName = "SA" },
                new Role { RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), RoleName = "CS" },
                new Role { RoleId = Guid.Parse("44444444-4444-4444-4444-444444444444"), RoleName = "DP" },
                new Role { RoleId = Guid.Parse("55555555-5555-5555-5555-555555555555"), RoleName = "TN" },
                new Role { RoleId = Guid.Parse("66666666-6666-6666-6666-666666666666"), RoleName = "QL" },
                new Role { RoleId = Guid.Parse("77777777-7777-7777-7777-777777777777"), RoleName = "QT" },
                new Role { RoleId = Guid.Parse("88888888-8888-8888-8888-888888888888"), RoleName = "BQL" }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Username).IsRequired();
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.ForcePasswordChange).HasDefaultValue(false);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.RoleId);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(x => x.ProfileId);
            entity.HasIndex(x => x.UserId).IsUnique();

            entity.HasOne(x => x.User)
                .WithOne(x => x.UserProfile)
                .HasForeignKey<UserProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.TokenId);
            entity.Property(x => x.Token).IsRequired();
            entity.HasIndex(x => x.UserId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TokenBlacklist>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenId).IsRequired();
            entity.HasIndex(x => x.TokenId).IsUnique();
        });

        // Apply all IEntityTypeConfiguration classes in this assembly (new entities)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

