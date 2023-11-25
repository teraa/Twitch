namespace Teraa.Twitch.PubSub.Messages.Shoutout;

public sealed record Shoutout(
    string Type,
    string BroadcasterUserId,
    string TargetUserId,
    string TargetLogin,
    string TargetUserProfileImageUrl,
    string SourceUserId,
    string SourceLogin,
    string ShoutoutId,
    string TargetUserDisplayName,
    string TargetUserCtaInfo,
    string TargetUserPrimaryColorHex);
