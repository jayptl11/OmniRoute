namespace OmniRoute.Infrastructure.Settings;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";
    public string ResendApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}

