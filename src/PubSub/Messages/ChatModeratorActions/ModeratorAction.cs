namespace Teraa.Twitch.PubSub.Messages.ChatModeratorActions;

public interface IModeratorAction : IMessage
{
    string Action { get; }
    string InitiatorId { get; }
}

public interface IInitiatorModeratorAction : IModeratorAction
{
    User Initiator { get; }

    string IModeratorAction.InitiatorId => Initiator.Id;
}

public interface ITargetedModeratorAction : IModeratorAction
{
    User Target { get; }
}

public interface ITermModeratorAction : IModeratorAction
{
    string Id { get; }
    string Text { get; }
    DateTimeOffset UpdatedAt { get; }
    string ChannelId { get; }
}

public record User(
    string Id,
    string Login
);

public record Ban(
    string Action,
    User Target,
    string Reason,
    DateTimeOffset CreatedAt,
    User Initiator
) : IInitiatorModeratorAction, ITargetedModeratorAction;

public record Unban(
    string Action,
    User Target,
    DateTimeOffset CreatedAt,
    User Initiator
) : IInitiatorModeratorAction, ITargetedModeratorAction;

public record Clear(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record Delete(
    string Action,
    User Target,
    DateTimeOffset CreatedAt,
    User Initiator,
    string MessageId,
    string Message
) : IInitiatorModeratorAction, ITargetedModeratorAction;

public record EmoteOnly(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record EmoteOnlyOff(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record Followers(
    string Action,
    TimeSpan Duration,
    User Initiator
) : IInitiatorModeratorAction;

public record FollowersOff(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record Raid(
    string Action,
    string TargetDisplayName,
    string InitiatorId,
    string InitiatorDisplayName
) : IModeratorAction;

public record Unraid(
    string Action,
    string InitiatorId,
    string InitiatorDisplayName
) : IModeratorAction;

public record Slow(
    string Action,
    TimeSpan Duration,
    User Initiator
) : IInitiatorModeratorAction;

public record SlowOff(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record Subscribers(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record SubscribersOff(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record R9KBeta(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record R9KBetaOff(
    string Action,
    User Initiator
) : IInitiatorModeratorAction;

public record Timeout(
    string Action,
    User Target,
    TimeSpan Duration,
    string Reason,
    DateTimeOffset CreatedAt,
    User Initiator
) : IInitiatorModeratorAction, ITargetedModeratorAction;

public record Untimeout(
    string Action,
    User Target,
    DateTimeOffset CreatedAt,
    User Initiator
) : IInitiatorModeratorAction, ITargetedModeratorAction;

// -----------------

public record Mod(
    string Action,
    User Target,
    User Initiator,
    string ChannelId
) : IInitiatorModeratorAction, ITargetedModeratorAction;

public record Unmod(
    string Action,
    User Target,
    User Initiator,
    string ChannelId
) : IInitiatorModeratorAction, ITargetedModeratorAction;

public record Vip(
    string Action,
    User Target,
    User Initiator,
    string ChannelId
) : IInitiatorModeratorAction, ITargetedModeratorAction;

// -----------------

public record ApproveUnbanRequest(
    string Action,
    User Target,
    string ModeratorMessage,
    User Initiator
) : IInitiatorModeratorAction, ITargetedModeratorAction;

public record DenyUnbanRequest(
    string Action,
    User Target,
    string ModeratorMessage,
    User Initiator
) : IInitiatorModeratorAction, ITargetedModeratorAction;

// -----------------

public record AddBlockedTerm(
    string Action,
    string Id,
    string Text,
    User Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : IInitiatorModeratorAction, ITermModeratorAction;

public record DeleteBlockedTerm(
    string Action,
    string Id,
    string Text,
    User Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : IInitiatorModeratorAction, ITermModeratorAction;

public record AddPermittedTerm(
    string Action,
    string Id,
    string Text,
    User Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : IInitiatorModeratorAction, ITermModeratorAction;

public record DeletePermittedTerm(
    string Action,
    string Id,
    string Text,
    User Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : IInitiatorModeratorAction, ITermModeratorAction;
