using System.Diagnostics.CodeAnalysis;

namespace Teraa.Twitch.PubSub.Topics;

public interface ITopic
{
    string Value { get; }
}

public record ChatModeratorActionsTopic(string UserId, string ChannelId) : ITopic
{
    public const string Name = "chat_moderator_actions";

    public string Value => $"{Name}.{UserId}.{ChannelId}";
}

public record ChannelUnbanRequestsTopic(string UserId, string ChannelId) : ITopic
{
    public const string Name = "channel-unban-requests";

    public string Value => $"{Name}.{UserId}.{ChannelId}";
}

public static class Topic
{
    public static bool TryParse(string input, [NotNullWhen(true)] out ITopic? topic)
    {
        var parts = input.Split('.');

        topic = parts[0] switch
        {
            ChatModeratorActionsTopic.Name when parts.Length == 3 => new ChatModeratorActionsTopic(parts[1], parts[2]),
            ChannelUnbanRequestsTopic.Name when parts.Length == 3 => new ChannelUnbanRequestsTopic(parts[1], parts[2]),
            _ => null,
        };

        return topic is not null;
    }
}
