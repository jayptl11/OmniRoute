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

        void SetAuditUserId(Guid userId);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

