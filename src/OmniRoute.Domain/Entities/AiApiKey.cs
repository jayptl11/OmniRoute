namespace OmniRoute.Domain.Entities;

public class AiApiKey
{
    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;   // "OpenAI" | "Gemini" | "Anthropic" | "Groq" | ...
    public string DisplayName { get; private set; } = string.Empty;
    public string EncryptedKey { get; private set; } = string.Empty;
    /// <summary>JSON object storing provider-specific params: model, temperature, maxTokens, etc.</summary>
    public string ConfigJson { get; private set; } = "{}";
    public int Priority { get; private set; }                      // 1 = highest, higher = lower priority
    public bool IsActive { get; private set; }
    public int FailureCount { get; private set; }
    public DateTime? LastFailedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AiApiKey() { } // EF Core

    public static AiApiKey Create(string provider, string displayName, string encryptedKey, string configJson, int priority, bool isActive = true)
    {
        return new AiApiKey
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            DisplayName = displayName,
            EncryptedKey = encryptedKey,
            ConfigJson = configJson,
            Priority = priority,
            IsActive = isActive,
            FailureCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateKey(string encryptedKey, string displayName, string configJson, int priority)
    {
        EncryptedKey = encryptedKey;
        DisplayName = displayName;
        ConfigJson = configJson;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMeta(string displayName, string configJson, int priority)
    {
        DisplayName = displayName;
        ConfigJson = configJson;
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

