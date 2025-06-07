namespace Teraa.Twitch.EventSub.Messages;

public sealed record Metadata(
    string MessageId,
    MessageType MessageType,
    DateTimeOffset MessageTimestamp,
    string? SubscriptionType,
    string? SubscriptionVersion
);

public enum MessageType
{
    SessionWelcome,
    SessionKeepalive,
    SessionReconnect,
    Notification,
    Revocation,
}

// TODO: JsonElement?
public sealed record Payload(
    Session? Session,
    Subscription? Subscription
    // Event? Event
);

public sealed record Session(
    string Id,
    string Status,
    int? KeepaliveTimeoutSeconds,
    string? ReconnectUrl,
    DateTimeOffset ConnectedAt
);


public sealed record Subscription(
    string Id,
    string Status,
    string Type,
    string Version,
    int Cost,
    Condition Condition,
    Transport Transport,
    DateTimeOffset CreatedAt
);

// TODO: JsonElement?
public sealed record Condition(
    string? BroadcasterId,
    string? BroadcasterUserId,
    string? CampaignId,
    string? CategoryId,
    string? ClientId,
    string? ConduitId,
    string? ExtensionClientId,
    string? FromBroadcasterUserId,
    string? ModeratorUserId,
    string? OrganizationId,
    string? RewardId,
    string? ToBroadcasterUserId,
    string? UserId
);

public sealed record Transport(
    string Method,
    string SessionId
);

public record BanEvent(
    string UserId,
    string UserLogin,
    string UserName,
    string BroadcasterUserId,
    string BroadcasterUserLogin,
    string BroadcasterUserName,
    string ModeratorUserId,
    string ModeratorUserLogin,
    string ModeratorUserName,
    string? Reason,
    string BannedAt,
    string? EndsAt,
    bool IsPermanent
);
