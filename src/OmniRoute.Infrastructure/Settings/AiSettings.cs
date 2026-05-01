namespace OmniRoute.Infrastructure.Settings;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    /// <summary>Base64-encoded 32-byte key for AES-256 encryption of API keys.</summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>Minimum confidence score from AI to use the result (default 0.65).</summary>
    public double ConfidenceThreshold { get; set; } = 0.65;

    /// <summary>HTTP timeout in seconds for AI provider calls (default 10).</summary>
    public int TimeoutSeconds { get; set; } = 10;
}
