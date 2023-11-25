using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JetBrains.Annotations;

namespace Teraa.Twitch.PubSub.Messages.Shoutout;

public static class Parser
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static bool TryParse(JsonDocument input, [NotNullWhen(true)] out Shoutout? result)
    {
        var message = input.Deserialize<ShoutoutMessage>(s_serializerOptions);

        if (message is null)
        {
            result = null;
            return false;
        }

        result = new Shoutout(
            message.Type,
            message.Data.BroadcasterUserID,
            message.Data.TargetUserID,
            message.Data.TargetLogin,
            message.Data.TargetUserProfileImageURL,
            message.Data.SourceUserID,
            message.Data.SourceLogin,
            message.Data.ShoutoutID,
            message.Data.TargetUserDisplayName,
            message.Data.TargetUserCTAInfo,
            message.Data.TargetUserPrimaryColorHex);

        return true;
    }

    [UsedImplicitly]
    private sealed record ShoutoutMessage(
        string Type,
        ShoutoutMessageData Data);

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private sealed record ShoutoutMessageData(
        string BroadcasterUserID,
        string TargetUserID,
        string TargetLogin,
        string TargetUserProfileImageURL,
        string SourceUserID,
        string SourceLogin,
        string ShoutoutID,
        string TargetUserDisplayName,
        string TargetUserCTAInfo,
        string TargetUserPrimaryColorHex);
}
