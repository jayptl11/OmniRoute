using System.Globalization;
using System.Text;
using System.Text.Json;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Domain.Services;

public static class RoutingRuleChannelHelper
{
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

    private static readonly Dictionary<string, Channel> ChannelAliases = new()
    {
        [CanonicalizeToken(nameof(Channel.Hotline))] = Channel.Hotline,
        [CanonicalizeToken(nameof(Channel.Walkin))] = Channel.Walkin,
        [CanonicalizeToken("Walk-in")] = Channel.Walkin,
        [CanonicalizeToken(nameof(Channel.Webform))] = Channel.Webform,
        [CanonicalizeToken("Web")] = Channel.Webform,
        [CanonicalizeToken(nameof(Channel.Chat))] = Channel.Chat,
        [CanonicalizeToken(nameof(Channel.Email))] = Channel.Email,
        [CanonicalizeToken(nameof(Channel.Zalo))] = Channel.Zalo,
        [CanonicalizeToken(nameof(Channel.Referral))] = Channel.Referral,
        [CanonicalizeToken("Gioi thieu")] = Channel.Referral
    };

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

            if (TryNormalizeChannel(channel, out var parsedChannel))
            {
                normalized.Add(parsedChannel.ToString());
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
            TryNormalizeChannel(token, out var parsedChannel) &&
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

        return TryNormalizeChannel(requestedChannel, out var parsedRequestChannel) &&
               ruleChannels.Any(token =>
                   TryNormalizeChannel(token, out var parsedRuleChannel) &&
                   parsedRuleChannel == parsedRequestChannel);
    }

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

    private static bool TryNormalizeChannel(string value, out Channel channel)
    {
        if (ChannelAliases.TryGetValue(CanonicalizeToken(value), out channel))
        {
            return true;
        }

        channel = default;
        return false;
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
}
