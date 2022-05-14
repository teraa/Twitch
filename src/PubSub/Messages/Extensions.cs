using System.Text.Json;

namespace Teraa.Twitch.PubSub.Messages;

internal static class Extensions
{
    public static string? GetPropertyString(this JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;

    public static DateTimeOffset? GetPropertyDateTimeOffset(this JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;

        if (!prop.TryGetDateTimeOffset(out var value))
            return null;

        return value;
    }
}
