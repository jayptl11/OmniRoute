namespace OmniRoute.Domain.Entities;

public class User
{
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLogin { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int CurrentWorkload { get; private set; }
    public Guid? RoleId { get; private set; }
    public Guid? TeamId { get; private set; }
    public Guid? StoreId { get; private set; }

    // Navigation properties — EF Core manages population via Include / relationship fixup
    public virtual Role? Role { get; set; }
    public virtual UserProfile? UserProfile { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    private User() { } // EF Core constructor

    public static User Create(
        Guid userId,
        string email,
        string username,
        string passwordHash,
        string? firstName = null,
        string? lastName = null,
        Guid? roleId = null) => new()
    {
        UserId = userId,
        Email = email,
        Username = username,
        PasswordHash = passwordHash,
        FirstName = firstName,
        LastName = lastName,
        CreatedAt = DateTime.UtcNow,
        IsActive = true,
        RoleId = roleId
    };

    public void UpdateLastLogin(DateTime timestamp) => LastLogin = timestamp;

    public void UpdatePassword(string passwordHash) => PasswordHash = passwordHash;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateDetails(string? firstName, string? lastName, string email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    public void AssignRole(Guid? roleId) => RoleId = roleId;

    public void AssignToTeam(Guid? teamId) => TeamId = teamId;

    public void AssignToStore(Guid? storeId) => StoreId = storeId;

    public void IncrementWorkload() => CurrentWorkload++;

    public void DecrementWorkload()
    {
        if (CurrentWorkload > 0)
            CurrentWorkload--;
    }
}

