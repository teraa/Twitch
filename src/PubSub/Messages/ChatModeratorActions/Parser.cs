﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Teraa.Twitch.PubSub.Messages.ChatModeratorActions;

// TODO: source generators
public static class Parser
{
    public static bool TryParse(JsonDocument input, [NotNullWhen(true)] out IModeratorAction? result)
    {
        if (!input.RootElement.TryGetProperty("type", out var typeElement) ||
            !input.RootElement.TryGetProperty("data", out var data))
        {
            result = null;
            return false;
        }

        var type = typeElement.GetString();

        if (type == "moderation_action")
            return TryParseModeratorAction(data, out result);

        var moderationAction = data.GetPropertyString("moderation_action");
        var createdById = data.GetPropertyString("created_by_id");
        var createdByLogin = data.GetPropertyString("created_by_login");
        var targetUserId = data.GetPropertyString("target_user_id");
        var targetUserLogin = data.GetPropertyString("target_user_login");
        var moderatorMessage = data.GetPropertyString("moderator_message");
        var channelId = data.GetPropertyString("channel_id");
        var createdByUserId = data.GetPropertyString("created_by_user_id");
        var createdBy = data.GetPropertyString("created_by");
        var innerType = data.GetPropertyString("type");

        switch (type)
        {
            case "approve_unban_request" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                moderatorMessage is { } &&
                createdById is { } &&
                createdByLogin is { }:

                result = new ApproveUnbanRequest(
                    Action: type,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    ModeratorMessage: moderatorMessage,
                    Initiator: new User(
                        Id: createdById,
                        Login: createdByLogin));
                return true;

            case "deny_unban_request" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                moderatorMessage is { } &&
                createdById is { } &&
                createdByLogin is { }:

                result = new DenyUnbanRequest(
                    Action: type,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    ModeratorMessage: moderatorMessage,
                    Initiator: new User(
                        Id: createdById,
                        Login: createdByLogin));
                return true;

            case "moderator_added" when
                moderationAction is { } &&
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdByUserId is { } &&
                createdBy is { } &&
                channelId is { }:

                result = new Mod(
                    Action: moderationAction,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy),
                    ChannelId: channelId);
                return true;

            case "moderator_removed" when
                moderationAction is { } &&
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdByUserId is { } &&
                createdBy is { } &&
                channelId is { }:

                result = new Unmod(
                    Action: moderationAction,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy),
                    ChannelId: channelId);
                return true;

            case "vip_added" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdByUserId is { } &&
                createdBy is { } &&
                channelId is { }:

                result = new Vip(
                    Action: type,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy),
                    ChannelId: channelId);
                return true;

            case "channel_terms_action":
            {
                var id = data.GetPropertyString("id");
                var text = data.GetPropertyString("text");
                var requesterId = data.GetPropertyString("requester_id");
                var requesterLogin = data.GetPropertyString("requester_login");

                if (channelId is null ||
                    id is null ||
                    text is null ||
                    requesterId is null ||
                    requesterLogin is null ||
                    !data.TryGetProperty("updated_at", out var updatedAtElement) ||
                    !updatedAtElement.TryGetDateTimeOffset(out var updatedAt))
                    break;

                switch (innerType)
                {
                    case "add_blocked_term":
                        result = new AddBlockedTerm(
                            Action: innerType,
                            Id: id,
                            Text: text,
                            Initiator: new User(
                                Id: requesterId,
                                Login: requesterLogin),
                            ChannelId: channelId,
                            UpdatedAt: updatedAt);
                        return true;

                    case "delete_blocked_term":
                        result = new DeleteBlockedTerm(
                            Action: innerType,
                            Id: id,
                            Text: text,
                            Initiator: new User(
                                Id: requesterId,
                                Login: requesterLogin),
                            ChannelId: channelId,
                            UpdatedAt: updatedAt);
                        return true;

                    case "add_permitted_term":
                        result = new AddPermittedTerm(
                            Action: innerType,
                            Id: id,
                            Text: text,
                            Initiator: new User(
                                Id: requesterId,
                                Login: requesterLogin),
                            ChannelId: channelId,
                            UpdatedAt: updatedAt);
                        return true;

                    case "delete_permitted_term":
                        result = new DeletePermittedTerm(
                            Action: innerType,
                            Id: id,
                            Text: text,
                            Initiator: new User(
                                Id: requesterId,
                                Login: requesterLogin),
                            ChannelId: channelId,
                            UpdatedAt: updatedAt);
                        return true;
                }

                break;
            }
        }

        result = null;
        return false;
    }

    private static bool TryParseModeratorAction(JsonElement data, [NotNullWhen(true)] out IModeratorAction? result)
    {
        JsonElement e;

        var args = data.TryGetProperty("args", out e)
            ? e.Deserialize<IReadOnlyList<string>>()
            : null;

        var createdAt = data.TryGetProperty("created_at", out e)
            ? e.TryGetDateTimeOffset(out var createdAtValue)
                ? (DateTimeOffset?) createdAtValue
                : null
            : null;

        var createdBy = data.GetPropertyString("created_by");
        var createdByUserId = data.GetPropertyString("created_by_user_id");
        var moderationAction = data.GetPropertyString("moderation_action");
        var targetUserId = data.GetPropertyString("target_user_id");
        var targetUserLogin = data.GetPropertyString("target_user_login");

        switch (moderationAction)
        {
            case "ban" when
                args is {Count: 2} &&
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdAt.HasValue &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Ban(
                    Action: moderationAction,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    Reason: args[1],
                    CreatedAt: createdAt.Value,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy
                    ));
                return true;

            case "unban" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdAt.HasValue &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Unban(
                    Action: moderationAction,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    CreatedAt: createdAt.Value,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy
                    ));
                return true;

            case "clear" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new Clear(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "delete" when
                targetUserId is { } &&
                args is {Count: 3} &&
                createdAt.HasValue &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Delete(
                    Action: moderationAction,
                    Target: new User(
                        Id: targetUserId,
                        Login: args[0]),
                    CreatedAt: createdAt.Value,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy),
                    MessageId: args[2],
                    Message: args[1]);
                return true;

            case "emoteonly" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new EmoteOnly(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "emoteonlyoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new EmoteOnlyOff(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "followers" when
                args is {Count: 1} &&
                int.TryParse(args[0], out var followersDuration) &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Followers(
                    Action: moderationAction,
                    Duration: TimeSpan.FromMinutes(followersDuration),
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "followersoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new FollowersOff(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            // TODO: automod_rejected

            case "raid" when
                args is {Count: 1} &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Raid(
                    Action: moderationAction,
                    TargetDisplayName: args[0],
                    InitiatorId: createdByUserId,
                    InitiatorDisplayName: createdBy);
                return true;

            case "unraid" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new Unraid(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    InitiatorDisplayName: createdBy);
                return true;

            case "slow" when
                args is {Count: 1} &&
                int.TryParse(args[0], out var followersDuration) &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Slow(
                    Action: moderationAction,
                    Duration: TimeSpan.FromSeconds(followersDuration),
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "slowoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new SlowOff(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "subscribers" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new Subscribers(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "subscribersoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new SubscribersOff(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "r9kbeta" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new R9KBeta(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "r9kbetaoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new R9KBetaOff(
                    Action: moderationAction,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "timeout" when
                args is {Count: 3} &&
                int.TryParse(args[1], out var timeoutDuration) &&
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdAt.HasValue &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Timeout(
                    Action: moderationAction,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    Duration: TimeSpan.FromSeconds(timeoutDuration),
                    Reason: args[2],
                    CreatedAt: createdAt.Value,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            case "untimeout" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdAt.HasValue &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Untimeout(
                    Action: moderationAction,
                    Target: new User(
                        Id: targetUserId,
                        Login: targetUserLogin),
                    CreatedAt: createdAt.Value,
                    Initiator: new User(
                        Id: createdByUserId,
                        Login: createdBy));
                return true;

            default:
                result = null;
                return false;
        }
    }
}
