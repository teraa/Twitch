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
    public void Ban_Add()
    {
        using var json = _repository.GetJson("ban_add.json");
        var success = Parser.TryParse(json, out var result);
        Assert.True(success);
        Assert.IsType<Ban>(result);
        var action = (Ban)result!;

        Assert.Equal("ban", action.Action);
        Assert.Equal("target.id", action.TargetId);
        Assert.Equal("target.login", action.Target);
        Assert.Equal("", action.Reason);
        Assert.Equal(new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), action.CreatedAt);
        Assert.Equal("initiator.id", action.InitiatorId);
        Assert.Equal("initiator.login", action.Initiator);
    }
}
