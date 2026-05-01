namespace OmniRoute.Domain.Entities;

public class AiApiKey
{
    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;   // "OpenAI" | "Gemini" | "Anthropic"
    public string DisplayName { get; private set; } = string.Empty;
    public string EncryptedKey { get; private set; } = string.Empty;
    public int Priority { get; private set; }  // 1 = primary, 2 = fallback
    public bool IsActive { get; private set; }
    public int FailureCount { get; private set; }
    public DateTime? LastFailedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AiApiKey() { } // EF Core

    public static AiApiKey Create(string provider, string displayName, string encryptedKey, int priority)
    {
        return new AiApiKey
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            DisplayName = displayName,
            EncryptedKey = encryptedKey,
            Priority = priority,
            IsActive = true,
            FailureCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateKey(string encryptedKey, string displayName, int priority)
    {
        EncryptedKey = encryptedKey;
        DisplayName = displayName;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMeta(string displayName, int priority)
    {
        DisplayName = displayName;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailure()
    {
        FailureCount++;
        LastFailedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSuccess()
    {
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
