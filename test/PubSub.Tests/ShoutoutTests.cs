using Teraa.Twitch.PubSub.Messages.Shoutout;

namespace PubSub.Tests;

public class ShoutoutTests : IClassFixture<SampleRepository>
{
    private readonly SampleRepository _repository;

    public ShoutoutTests(SampleRepository repository)
    {
        _repository = repository;
    }

    [Fact]
    public void Shoutout()
    {
        using var json = _repository.GetJson("shoutout.json");

        var success = Parser.TryParse(json, out var result);

        success.Should().BeTrue();
        result.Should().NotBeNull();

        result!.Type.Should().Be("create");
        result.BroadcasterUserId.Should().Be("channel.id");
        result.TargetUserId.Should().Be("target.id");
        result.TargetLogin.Should().Be("target.login");
        result.TargetUserProfileImageUrl.Should().Be("https://static-cdn.jtvnw.net/jtv_user_pictures/00000000-0000-0000-0000-000000000000-profile_image-%s.jpeg");
        result.SourceUserId.Should().Be("initiator.id");
        result.SourceLogin.Should().Be("initiator.login");
        result.ShoutoutId.Should().Be("00000000-0000-0000-0000-000000000000");
        result.TargetUserDisplayName.Should().Be("target.display");
        result.TargetUserCtaInfo.Should().BeEmpty();
        result.TargetUserPrimaryColorHex.Should().Be("B69C00");
    }
}
