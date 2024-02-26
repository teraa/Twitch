using System.Collections.Generic;
using Teraa.Twitch.PubSub.Topics;

namespace PubSub.Tests;

public class TopicTests
{
    public static IEnumerable<object[]> Parse_Topic_Data() => new[]
    {
        new object[] {"chat_moderator_actions.user_id.channel_id", new ChatModeratorActionsTopic("user_id", "channel_id")},
        new object[] {"channel-unban-requests.user_id.channel_id", new ChannelUnbanRequestsTopic("user_id", "channel_id")},
        new object[] {"shoutout.channel_id", new ShoutoutTopic("channel_id")},
        new object[] {"low-trust-users.user_id.channel_id", new LowTrustUsersTopic("user_id", "channel_id")},
    };

    [Theory]
    [MemberData(nameof(Parse_Topic_Data))]
    public void Parse_Topic(string input, ITopic expectedTopic)
    {
        var success = Topic.TryParse(input, out var topic);

        success.Should().BeTrue();
        topic.Should().Be(expectedTopic);
    }
}
