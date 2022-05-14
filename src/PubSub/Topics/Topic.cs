using System.Diagnostics.CodeAnalysis;

namespace Teraa.Twitch.PubSub.Topics;

public interface ITopic
{
    string Value { get; }
}

public record ChatModeratorActionsTopic(string UserId, string ChannelId) : ITopic
{
    public const string Prefix = "chat_moderator_actions";

    public string Value => $"{Prefix}.{UserId}.{ChannelId}";
}

public record ChannelUnbanRequestsTopic(string UserId, string ChannelId) : ITopic
{
    public const string Prefix = "channel-unban-requests";

    public string Value => $"{Prefix}.{UserId}.{ChannelId}";
}

public static class Topic
{
    public static bool TryParse(string input, [NotNullWhen(true)] out ITopic? topic)
    {
        var parts = input.Split('.');

        topic = parts[0] switch
        {
            ChatModeratorActionsTopic.Prefix when parts.Length == 3 => new ChatModeratorActionsTopic(parts[1], parts[2]),
            ChannelUnbanRequestsTopic.Prefix when parts.Length == 3 => new ChannelUnbanRequestsTopic(parts[1], parts[2]),
            _ => null,
        };

        return topic is not null;
    }
}
