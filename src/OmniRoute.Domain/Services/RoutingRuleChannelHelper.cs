using System.Globalization;
using System.Text;
using System.Text.Json;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Services;

public static class RoutingRuleChannelHelper
{
    private sealed record ChannelDefinition(Channel Channel, string DisplayName, string[] Aliases);

    private static readonly ChannelDefinition[] ChannelDefinitions =
    [
        new(Channel.Hotline, nameof(Channel.Hotline), [nameof(Channel.Hotline)]),
        new(Channel.Walkin, nameof(Channel.Walkin), [nameof(Channel.Walkin), "Walk-in", "Truc tiep tai cua hang"]),
        new(Channel.Webform, nameof(Channel.Webform), [nameof(Channel.Webform), "Web", "Website", "Bieu mau web"]),
        new(Channel.Chat, nameof(Channel.Chat), [nameof(Channel.Chat)]),
        new(Channel.Email, nameof(Channel.Email), [nameof(Channel.Email)]),
        new(Channel.Zalo, nameof(Channel.Zalo), [nameof(Channel.Zalo)]),
        new(Channel.Referral, nameof(Channel.Referral), [nameof(Channel.Referral), "Gioi thieu"])
    ];

    private static readonly HashSet<string> WildcardTokens =
    [
        "tatcakenh",
        "all",
        "allchannel",
        "allchannels",
        "any",
        "anychannel",
        "anychannels"
    ];

    private static readonly Dictionary<string, ChannelDefinition> ChannelDefinitionsByToken = BuildDefinitionsByToken();

    private static readonly Dictionary<Channel, ChannelDefinition> ChannelDefinitionsByValue =
        ChannelDefinitions.ToDictionary(definition => definition.Channel);

    public static List<string>? NormalizeConditionChannels(IEnumerable<string>? channels)
    {
        if (channels is null)
        {
            return null;
        }

        var normalized = new List<string>();

        foreach (var channel in channels)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                continue;
            }

            if (IsWildcardToken(channel))
            {
                return null;
            }

            if (TryParseChannel(channel, out var parsedChannel))
            {
                normalized.Add(GetCanonicalName(parsedChannel));
                continue;
            }

            normalized.Add(channel.Trim());
        }

        return normalized.Count == 0
            ? null
            : normalized.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static bool RuleMatchesLeadChannel(string? conditionChannelJson, Channel leadChannel)
    {
        var ruleChannels = DeserializeChannelTokens(conditionChannelJson);
        if (IsWildcardRule(ruleChannels))
        {
            return true;
        }

        return ruleChannels.Any(token =>
            TryParseChannel(token, out var parsedChannel) &&
            parsedChannel == leadChannel);
    }

    public static bool RuleMatchesRequestedChannel(string? conditionChannelJson, string? requestedChannel)
    {
        var ruleChannels = DeserializeChannelTokens(conditionChannelJson);
        if (IsWildcardRule(ruleChannels))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(requestedChannel) || IsWildcardToken(requestedChannel))
        {
            return false;
        }

        return TryParseChannel(requestedChannel, out var parsedRequestChannel) &&
               ruleChannels.Any(token =>
                   TryParseChannel(token, out var parsedRuleChannel) &&
                   parsedRuleChannel == parsedRequestChannel);
    }

    public static bool TryParseChannel(string? value, out Channel channel)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            ChannelDefinitionsByToken.TryGetValue(CanonicalizeToken(value), out var definition))
        {
            channel = definition.Channel;
            return true;
        }

        channel = default;
        return false;
    }

    public static string GetCanonicalName(Channel channel)
        => ChannelDefinitionsByValue[channel].Channel.ToString();

    public static string? NormalizeRequestedChannel(string? requestedChannel)
        => TryParseChannel(requestedChannel, out var channel) ? GetCanonicalName(channel) : null;

    public static string GetDisplayName(Channel channel)
        => ChannelDefinitionsByValue[channel].DisplayName;

    public static string? GetDisplayName(string? channel)
        => TryParseChannel(channel, out var parsedChannel) ? GetDisplayName(parsedChannel) : null;

    private static bool IsWildcardRule(IReadOnlyCollection<string> channels)
        => channels.Count == 0 || channels.Any(IsWildcardToken);

    private static List<string> DeserializeChannelTokens(string? conditionChannelJson)
    {
        if (string.IsNullOrWhiteSpace(conditionChannelJson))
        {
            return [];
        }

        try
        {
            if (conditionChannelJson.TrimStart().StartsWith("[", StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<List<string>>(conditionChannelJson) ?? [];
            }

            if (conditionChannelJson.TrimStart().StartsWith("\"", StringComparison.Ordinal))
            {
                var singleValue = JsonSerializer.Deserialize<string>(conditionChannelJson);
                return string.IsNullOrWhiteSpace(singleValue) ? [] : [singleValue];
            }
        }
        catch (JsonException)
        {
            // Fall back to treating the stored value as a raw token.
        }

        return [conditionChannelJson];
    }

    private static bool IsWildcardToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value.Trim() == "*")
        {
            return true;
        }

        return WildcardTokens.Contains(CanonicalizeToken(value));
    }

    private static string CanonicalizeToken(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static Dictionary<string, ChannelDefinition> BuildDefinitionsByToken()
    {
        var result = new Dictionary<string, ChannelDefinition>(StringComparer.Ordinal);

        foreach (var definition in ChannelDefinitions)
        {
            foreach (var alias in definition.Aliases.Append(definition.DisplayName))
            {
                result[CanonicalizeToken(alias)] = definition;
            }
        }

        return result;
    }
}
