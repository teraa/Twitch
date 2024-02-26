namespace Teraa.Twitch.PubSub.Messages.LowTrustUsers;

public sealed record TreatmentUpdate(
    string LowTrustId,
    string ChannelId,
    User UpdatedBy,
    DateTimeOffset UpdatedAt,
    string TargetUserId,
    string TargetUser,
    string Treatment,
    IReadOnlyList<string> Types,
    string BanEvasionEvaluation,
    DateTimeOffset EvaluatedAt
);

public sealed record User(
    string Id,
    string Login,
    string DisplayName
);
