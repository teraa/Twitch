namespace Teraa.Twitch.PubSub.Messages.ChatModeratorActions;

public interface IModeratorAction : IMessage
{
    string Action { get; }
    string InitiatorId { get; }
    string Initiator { get; }
}

public interface ITargetedModeratorAction : IModeratorAction
{
    string TargetId { get; }
    string Target { get; }
}

public interface ITermModeratorAction : IModeratorAction
{
    string Id { get; }
    string Text { get; }
    DateTimeOffset UpdatedAt { get; }
    string ChannelId { get; }
}

public record Ban(
    string Action,
    string TargetId,
    string Target,
    string Reason,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator
) : ITargetedModeratorAction;

public record Unban(
    string Action,
    string TargetId,
    string Target,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator
) : ITargetedModeratorAction;

public record Clear(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record Delete(
    string Action,
    string TargetId,
    string Target,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator,
    string MessageId,
    string Message
) : ITargetedModeratorAction;

public record EmoteOnly(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record EmoteOnlyOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record Followers(
    string Action,
    TimeSpan Duration,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record FollowersOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record Raid(
    string Action,
    string Target,
    string InitiatorId,
    string Initiator // Not login
) : IModeratorAction;

public record Unraid(
    string Action,
    string InitiatorId,
    string Initiator // Not login
) : IModeratorAction;

public record Slow(
    string Action,
    TimeSpan Duration,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record SlowOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record Subscribers(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record SubscribersOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record R9KBeta(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record R9KBetaOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IModeratorAction;

public record Timeout(
    string Action,
    string TargetId,
    string Target,
    TimeSpan Duration,
    string Reason,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator
) : ITargetedModeratorAction;

public record Untimeout(
    string Action,
    string TargetId,
    string Target,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator
) : ITargetedModeratorAction;

// -----------------

public record Mod(
    string Action,
    string TargetId,
    string Target,
    string InitiatorId,
    string Initiator,
    string ChannelId
) : ITargetedModeratorAction;

public record Unmod(
    string Action,
    string TargetId,
    string Target,
    string InitiatorId,
    string Initiator,
    string ChannelId
) : ITargetedModeratorAction;

public record Vip(
    string Action,
    string TargetId,
    string Target,
    string InitiatorId,
    string Initiator,
    string ChannelId
) : ITargetedModeratorAction;

// -----------------

public record ApproveUnbanRequest(
    string Action,
    string TargetId,
    string Target,
    string ModeratorMessage,
    string InitiatorId,
    string Initiator
) : ITargetedModeratorAction;

public record DenyUnbanRequest(
    string Action,
    string TargetId,
    string Target,
    string ModeratorMessage,
    string InitiatorId,
    string Initiator
) : ITargetedModeratorAction;

// -----------------

public record AddBlockedTerm(
    string Action,
    string Id,
    string Text,
    string InitiatorId,
    string Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : ITermModeratorAction;

public record DeleteBlockedTerm(
    string Action,
    string Id,
    string Text,
    string InitiatorId,
    string Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : ITermModeratorAction;

public record AddPermittedTerm(
    string Action,
    string Id,
    string Text,
    string InitiatorId,
    string Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : ITermModeratorAction;

public record DeletePermittedTerm(
    string Action,
    string Id,
    string Text,
    string InitiatorId,
    string Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : ITermModeratorAction;
