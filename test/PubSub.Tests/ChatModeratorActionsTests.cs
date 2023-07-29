using System;
using Teraa.Twitch.PubSub.Messages.ChatModeratorActions;
using Xunit;

namespace PubSub.Tests;

public class ChatModeratorActionsTests : IClassFixture<ChatModeratorActionsSampleRepository>
{
    private readonly ChatModeratorActionsSampleRepository _repository;

    public ChatModeratorActionsTests(ChatModeratorActionsSampleRepository repository)
    {
        _repository = repository;
    }

    [Fact]
    public void Automod_Blocked_Term_Add()
    {
        using var json = _repository.GetJson("automod_blocked_term_add.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<AddBlockedTerm>(result);
        var action = (AddBlockedTerm)result!;

        Assert.Equal("add_blocked_term", action.Action);
        Assert.Equal("00000000-0000-0000-0000-000000000001", action.Id);
        Assert.Equal("block phrase", action.Text);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
        Assert.Equal("channel.id", action.ChannelId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.UpdatedAt);
    }

    [Fact]
    public void Automod_Blocked_Term_Remove()
    {
        using var json = _repository.GetJson("automod_blocked_term_remove.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<DeleteBlockedTerm>(result);
        var action = (DeleteBlockedTerm)result!;

        Assert.Equal("delete_blocked_term", action.Action);
        Assert.Equal("00000000-0000-0000-0000-000000000001", action.Id);
        Assert.Equal("block phrase", action.Text);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
        Assert.Equal("channel.id", action.ChannelId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.UpdatedAt);
    }

    [Fact]
    public void Automod_Permitted_Term_Add()
    {
        using var json = _repository.GetJson("automod_permitted_term_add.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<AddPermittedTerm>(result);
        var action = (AddPermittedTerm)result!;

        Assert.Equal("add_permitted_term", action.Action);
        Assert.Equal("00000000-0000-0000-0000-000000000002", action.Id);
        Assert.Equal("permit phrase", action.Text);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
        Assert.Equal("channel.id", action.ChannelId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.UpdatedAt);
    }

    [Fact]
    public void Automod_Permitted_Term_Remove()
    {
        using var json = _repository.GetJson("automod_permitted_term_remove.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<DeletePermittedTerm>(result);
        var action = (DeletePermittedTerm)result!;

        Assert.Equal("delete_permitted_term", action.Action);
        Assert.Equal("00000000-0000-0000-0000-000000000002", action.Id);
        Assert.Equal("permit phrase", action.Text);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
        Assert.Equal("channel.id", action.ChannelId);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.UpdatedAt);
    }

    [Fact]
    public void Ban_Add()
    {
        using var json = _repository.GetJson("ban_add.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Ban>(result);
        var action = (Ban)result!;

        Assert.Equal("ban", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal("", action.Reason);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.CreatedAt);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Ban_Add_Reason()
    {
        using var json = _repository.GetJson("ban_add_reason.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Ban>(result);
        var action = (Ban)result!;

        Assert.Equal("ban", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal("reason", action.Reason);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.CreatedAt);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Ban_Remove()
    {
        using var json = _repository.GetJson("ban_remove.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Unban>(result);
        var action = (Unban)result!;

        Assert.Equal("unban", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.CreatedAt);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Clear()
    {
        using var json = _repository.GetJson("clear.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Clear>(result);
        var action = (Clear)result!;

        Assert.Equal("clear", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Delete()
    {
        using var json = _repository.GetJson("delete.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Delete>(result);
        var action = (Delete)result!;

        Assert.Equal("delete", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal(new DateTimeOffset(2022, 6, 29, 14, 0, 0, TimeSpan.Zero), action.CreatedAt);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
        Assert.Equal("00000000-0000-0000-0000-000000000003", action.MessageId);
        Assert.Equal("message", action.Message);
    }

    [Fact]
    public void Emoteonly_Enable()
    {
        using var json = _repository.GetJson("emoteonly_enable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<EmoteOnly>(result);
        var action = (EmoteOnly)result!;

        Assert.Equal("emoteonly", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Emoteonly_Disable()
    {
        using var json = _repository.GetJson("emoteonly_disable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<EmoteOnlyOff>(result);
        var action = (EmoteOnlyOff)result!;

        Assert.Equal("emoteonlyoff", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Followers_Disable()
    {
        using var json = _repository.GetJson("followers_disable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<FollowersOff>(result);
        var action = (FollowersOff)result!;

        Assert.Equal("followersoff", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Followers_Enable_0()
    {
        using var json = _repository.GetJson("followers_enable_0.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Followers>(result);
        var action = (Followers)result!;

        Assert.Equal("followers", action.Action);
        Assert.Equal(TimeSpan.Zero, action.Duration);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Followers_Enable_1d()
    {
        using var json = _repository.GetJson("followers_enable_1d.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Followers>(result);
        var action = (Followers)result!;

        Assert.Equal("followers", action.Action);
        Assert.Equal(TimeSpan.FromDays(1), action.Duration);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Mod_Add()
    {
        using var json = _repository.GetJson("mod_add.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Mod>(result);
        var action = (Mod)result!;

        Assert.Equal("mod", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
        Assert.Equal("channel.id", action.ChannelId);
    }

    [Fact]
    public void Raid_Disable()
    {
        using var json = _repository.GetJson("raid_disable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Unraid>(result);
        var action = (Unraid)result!;

        Assert.Equal("unraid", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.name", action.InitiatorDisplayName);
    }

    [Fact]
    public void Raid_Enable()
    {
        using var json = _repository.GetJson("raid_enable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Raid>(result);
        var action = (Raid)result!;

        Assert.Equal("raid", action.Action);
        Assert.Equal("target.name", action.TargetDisplayName);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.name", action.InitiatorDisplayName);
    }

    [Fact]
    public void Slow_Disable()
    {
        using var json = _repository.GetJson("slow_disable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<SlowOff>(result);
        var action = (SlowOff)result!;

        Assert.Equal("slowoff", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Slow_Enable()
    {
        using var json = _repository.GetJson("slow_enable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Slow>(result);
        var action = (Slow)result!;

        Assert.Equal("slow", action.Action);
        Assert.Equal(TimeSpan.FromSeconds(30), action.Duration);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Subscribers_Disable()
    {
        using var json = _repository.GetJson("subscribers_disable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<SubscribersOff>(result);
        var action = (SubscribersOff)result!;

        Assert.Equal("subscribersoff", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Subscribers_Enable()
    {
        using var json = _repository.GetJson("subscribers_enable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Subscribers>(result);
        var action = (Subscribers)result!;

        Assert.Equal("subscribers", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Timeout_Add_10m()
    {
        using var json = _repository.GetJson("timeout_add_10m.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Timeout>(result);
        var action = (Timeout)result!;

        Assert.Equal("timeout", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal(TimeSpan.FromMinutes(10), action.Duration);
        Assert.Equal("", action.Reason);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.CreatedAt);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Timeout_Add_1d_Reason()
    {
        using var json = _repository.GetJson("timeout_add_1d_reason.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Timeout>(result);
        var action = (Timeout)result!;

        Assert.Equal("timeout", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal(TimeSpan.FromDays(1), action.Duration);
        Assert.Equal("reason", action.Reason);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.CreatedAt);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Timeout_Remove()
    {
        using var json = _repository.GetJson("timeout_remove.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Untimeout>(result);
        var action = (Untimeout)result!;

        Assert.Equal("untimeout", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.CreatedAt);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Unban_Request_Approve()
    {
        using var json = _repository.GetJson("unban_request_approve.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<ApproveUnbanRequest>(result);
        var action = (ApproveUnbanRequest)result!;

        Assert.Equal("approve_unban_request", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal("approved response", action.ModeratorMessage);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Unban_Request_Deny()
    {
        using var json = _repository.GetJson("unban_request_deny.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<DenyUnbanRequest>(result);
        var action = (DenyUnbanRequest)result!;

        Assert.Equal("deny_unban_request", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.TargetLogin);
        Assert.Equal("denied response", action.ModeratorMessage);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Unique_Disable()
    {
        using var json = _repository.GetJson("unique_disable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<R9KBetaOff>(result);
        var action = (R9KBetaOff)result!;

        Assert.Equal("r9kbetaoff", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }

    [Fact]
    public void Unique_Enable()
    {
        using var json = _repository.GetJson("unique_enable.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<R9KBeta>(result);
        var action = (R9KBeta)result!;

        Assert.Equal("r9kbeta", action.Action);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.InitiatorLogin);
    }
}
