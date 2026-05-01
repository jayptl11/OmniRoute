using OmniRoute.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace OmniRoute.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Role> Roles { get; }
        DbSet<User> Users { get; }
        DbSet<UserProfile> UserProfiles { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<TokenBlacklist> TokenBlacklist { get; }

        // Lead management
        DbSet<Lead> Leads { get; }
        DbSet<Ticket> Tickets { get; }
        DbSet<Store> Stores { get; }
        DbSet<Team> Teams { get; }
        DbSet<RoutingRule> RoutingRules { get; }
        DbSet<ActivityLog> ActivityLogs { get; }
        DbSet<SlaConfig> SlaConfigs { get; }
        DbSet<Notification> Notifications { get; }
        DbSet<MasterDataItem> MasterDataItems { get; }
        DbSet<FollowUpTask> FollowUpTasks { get; }
        DbSet<NotificationConfig> NotificationConfigs { get; }
        DbSet<AiApiKey> AiApiKeys { get; }

        void SetAuditUserId(Guid userId);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

