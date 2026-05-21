using System.Text.Json;
using System.Text.Json.Serialization;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Services;

namespace OmniRoute.API.Serialization;

internal sealed class CanonicalChannelJsonConverter : JsonConverter<Channel>
{
    public override Channel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Channel must be provided as a string.");
        }

        var rawValue = reader.GetString();
        if (RoutingRuleChannelHelper.TryParseChannel(rawValue, out var channel))
        {
            return channel;
        }

        throw new JsonException(
            $"Channel '{rawValue}' is not valid. Valid values: {string.Join(", ", Enum.GetNames<Channel>())}");
    }

    public override void Write(Utf8JsonWriter writer, Channel value, JsonSerializerOptions options)
        => writer.WriteStringValue(RoutingRuleChannelHelper.GetCanonicalName(value));
}
