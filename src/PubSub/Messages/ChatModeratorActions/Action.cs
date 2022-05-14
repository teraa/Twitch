namespace Teraa.Twitch.PubSub.Messages.ChatModeratorActions;

public interface IAction
{
    string Action { get; }
    string InitiatorId { get; }
    string Initiator { get; }
}

public interface ITargetedAction : IAction
{
    string TargetId { get; }
    string Target { get; }
}

public interface ITermAction : IAction
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
    string? Reason,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator
) : ITargetedAction;

public record Unban(
    string Action,
    string TargetId,
    string Target,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator
) : ITargetedAction;

public record Clear(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record EmoteOnly(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record EmoteOnlyOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record Followers(
    string Action,
    TimeSpan Duration,
    string InitiatorId,
    string Initiator
) : IAction;

public record FollowersOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record Raid(
    string Action,
    string Target,
    string InitiatorId,
    string Initiator // Not login
) : IAction;

public record Unraid(
    string Action,
    string InitiatorId,
    string Initiator // Not login
) : IAction;

public record Slow(
    string Action,
    TimeSpan Duration,
    string InitiatorId,
    string Initiator
) : IAction;

public record SlowOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record Subscribers(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record SubscribersOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record R9KBeta(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record R9KBetaOff(
    string Action,
    string InitiatorId,
    string Initiator
) : IAction;

public record Timeout(
    string Action,
    string TargetId,
    string Target,
    TimeSpan Duration,
    string? Reason,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator
) : ITargetedAction;

public record Untimeout(
    string Action,
    string TargetId,
    string Target,
    DateTimeOffset CreatedAt,
    string InitiatorId,
    string Initiator
) : ITargetedAction;

// -----------------

public record Mod(
    string Action,
    string TargetId,
    string Target,
    string InitiatorId,
    string Initiator,
    string ChannelId
) : ITargetedAction;

public record Unmod(
    string Action,
    string TargetId,
    string Target,
    string InitiatorId,
    string Initiator,
    string ChannelId
) : ITargetedAction;

public record Vip(
    string Action,
    string TargetId,
    string Target,
    string InitiatorId,
    string Initiator,
    string ChannelId
) : ITargetedAction;

// -----------------

public record ApproveUnbanRequest(
    string Action,
    string TargetId,
    string Target,
    string? ModeratorMessage,
    string InitiatorId,
    string Initiator
) : ITargetedAction;

public record DenyUnbanRequest(
    string Action,
    string TargetId,
    string Target,
    string? ModeratorMessage,
    string InitiatorId,
    string Initiator
) : ITargetedAction;

// -----------------

public record AddBlockedTerm(
    string Action,
    string Id,
    string Text,
    string InitiatorId,
    string Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : ITermAction;

public record DeleteBlockedTerm(
    string Action,
    string Id,
    string Text,
    string InitiatorId,
    string Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : ITermAction;

public record AddPermittedTerm(
    string Action,
    string Id,
    string Text,
    string InitiatorId,
    string Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : ITermAction;

public record DeletePermittedTerm(
    string Action,
    string Id,
    string Text,
    string InitiatorId,
    string Initiator,
    string ChannelId,
    DateTimeOffset UpdatedAt
) : ITermAction;
