using System;
using Teraa.Twitch.PubSub.Messages.LowTrustUsers;

namespace PubSub.Tests;

public class LowTrustUsersTests : IClassFixture<SampleRepository>
{
    private readonly SampleRepository _repository;

    public LowTrustUsersTests(SampleRepository repository)
    {
        _repository = repository;
    }

    [Fact]
    public void TreatmentUpdate()
    {
        using var json = _repository.GetJson("low_trust_user_treatment_update.json");

        var success = Parser.TryParse(json, out var result);

        success.Should().BeTrue();
        result.Should().NotBeNull();

        result!.LowTrustId.Should().Be("channel_id.target_id");
        result.ChannelId.Should().Be("channel_id");
        result.UpdatedBy.Id.Should().Be("initiator_id");
        result.UpdatedBy.Login.Should().Be("initiator_login");
        result.UpdatedBy.DisplayName.Should().Be("initiator_name");
        result.UpdatedAt.Should().Be(new DateTimeOffset(2024, 2, 1, 1, 0, 0, TimeSpan.Zero));
        result.TargetUserId.Should().Be("target_id");
        result.TargetUser.Should().Be("target_login");
        result.Treatment.Should().Be("ACTIVE_MONITORING");
        result.Types.Should().BeEquivalentTo("MANUALLY_ADDED");
        result.BanEvasionEvaluation.Should().Be("UNLIKELY_EVADER");
        result.EvaluatedAt.Should().Be(new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero));
    }
}
