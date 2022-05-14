﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Teraa.Twitch.PubSub.Messages.ChatModeratorActions;

// TODO: source generators
public static class Message
{
    public static bool TryParse(JsonDocument input, [NotNullWhen(true)] out IAction? result)
    {
        if (!input.RootElement.TryGetProperty("type", out var typeElement) ||
            !input.RootElement.TryGetProperty("data", out var data))
        {
            result = null;
            return false;
        }

        var type = typeElement.GetString();

        if (type == "moderation_action")
            return TryParse(data, out result);

        var moderationAction = Get(data, "moderation_action");
        var createdById = Get(data, "created_by_id");
        var createdByLogin = Get(data, "created_by_login");
        var targetUserId = Get(data, "target_user_id");
        var targetUserLogin = Get(data, "target_user_login");
        var moderatorMessage = Get(data, "moderator_message");
        var channelId = Get(data, "channel_id");
        var createdByUserId = Get(data, "created_by_user_id");
        var createdBy = Get(data, "created_by");
        var innerType = Get(data, "type");

        switch (type)
        {
            case "approve_unban_request" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                moderatorMessage is { } &&
                createdById is { } &&
                createdByLogin is { }:
            {
                result = new ApproveUnbanRequest(
                    Action: type,
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    ModeratorMessage: moderatorMessage.Length > 0 ? moderatorMessage : null,
                    InitiatorId: createdById,
                    Initiator: createdByLogin);

                return true;
            }

            case "deny_unban_request" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                moderatorMessage is { } &&
                createdById is { } &&
                createdByLogin is { }:
            {
                result = new DenyUnbanRequest(
                    Action: type,
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    ModeratorMessage: moderatorMessage.Length > 0 ? moderatorMessage : null,
                    InitiatorId: createdById,
                    Initiator: createdByLogin);

                return true;
            }

            case "moderator_added" when
                moderationAction is { } &&
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdByUserId is { } &&
                createdBy is { } &&
                channelId is { }:
            {
                result = new Mod(
                    Action: moderationAction,
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy,
                    ChannelId: channelId);
                return true;
            }

            case "moderator_removed" when
                moderationAction is { } &&
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdByUserId is { } &&
                createdBy is { } &&
                channelId is { }:
            {
                result = new Unmod(
                    Action: moderationAction,
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy,
                    ChannelId: channelId);
                return true;
            }

            case "vip_added" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdByUserId is { } &&
                createdBy is { } &&
                channelId is { }:
            {
                result = new Vip(
                    Action: type,
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy,
                    ChannelId: channelId);
                return true;
            }

            case "channel_terms_action":
            {
                var id = Get(data, "id");
                var text = Get(data, "text");
                var requesterId = Get(data, "requester_id");
                var requesterLogin = Get(data, "requester_login");

                if (channelId is null ||
                    id is null ||
                    text is null ||
                    requesterId is null ||
                    requesterLogin is null ||
                    !data.TryGetProperty("updated_at", out var updatedAtElement) ||
                    !updatedAtElement.TryGetDateTimeOffset(out var updatedAt))
                {
                    result = null;
                    return false;
                }

                switch (innerType)
                {
                    case "add_blocked_term":
                        result = new AddBlockedTerm(
                            Action: innerType,
                            Id: id,
                            Text: text,
                            InitiatorId: requesterId,
                            Initiator: requesterLogin,
                            ChannelId: channelId,
                            UpdatedAt: updatedAt);
                        return true;

                    case "delete_blocked_term":
                        result = new DeleteBlockedTerm(
                            Action: innerType,
                            Id: id,
                            Text: text,
                            InitiatorId: requesterId,
                            Initiator: requesterLogin,
                            ChannelId: channelId,
                            UpdatedAt: updatedAt);
                        return true;

                    case "add_permitted_term":
                        result = new AddPermittedTerm(
                            Action: innerType,
                            Id: id,
                            Text: text,
                            InitiatorId: requesterId,
                            Initiator: requesterLogin,
                            ChannelId: channelId,
                            UpdatedAt: updatedAt);
                        return true;

                    case "delete_permitted_term":
                        result = new DeletePermittedTerm(
                            Action: innerType,
                            Id: id,
                            Text: text,
                            InitiatorId: requesterId,
                            Initiator: requesterLogin,
                            ChannelId: channelId,
                            UpdatedAt: updatedAt);
                        return true;

                    default:
                        result = null;
                        return false;
                }
            }

            default:
                result = null;
                return false;
        }
    }

    public static bool TryParse(JsonElement data, [NotNullWhen(true)] out IAction? result)
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

        var createdBy = Get(data, "created_by");
        var createdByUserId = Get(data, "created_by_user_id");
        var moderationAction = Get(data, "moderation_action");
        var targetUserId = Get(data, "target_user_id");
        var targetUserLogin = Get(data, "target_user_login");
        var type = Get(data, "type");

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
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    Reason: NullIfEmpty(args[1]),
                    CreatedAt: createdAt.Value,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy
                );
                return true;

            case "unban" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdAt.HasValue &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Unban(
                    Action: moderationAction,
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    CreatedAt: createdAt.Value,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy
                );
                return true;

            case "clear" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new Clear(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "emoteonly" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new EmoteOnly(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "emoteonlyoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new EmoteOnlyOff(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "followers" when
                args is {Count: 1} &&
                int.TryParse(args[0], out var followersDuration) &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Followers(
                    Action: moderationAction,
                    Duration: TimeSpan.FromSeconds(followersDuration),
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "followersoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new FollowersOff(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            // TODO: automod_rejected

            case "raid" when
                args is {Count: 1} &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Raid(
                    Action: moderationAction,
                    Target: args[0],
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "unraid" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new Unraid(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "slow" when
                args is {Count: 1} &&
                int.TryParse(args[0], out var followersDuration) &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Slow(
                    Action: moderationAction,
                    Duration: TimeSpan.FromSeconds(followersDuration),
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "slowoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new SlowOff(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "subscribers" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new Subscribers(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "subscribersoff" when
                createdByUserId is { } &&
                createdBy is { }:

                result = new SubscribersOff(
                    Action: moderationAction,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
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
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    Duration: TimeSpan.FromSeconds(timeoutDuration),
                    Reason: NullIfEmpty(args[2]),
                    CreatedAt: createdAt.Value,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            case "untimeout" when
                targetUserId is { } &&
                targetUserLogin is { } &&
                createdAt.HasValue &&
                createdByUserId is { } &&
                createdBy is { }:

                result = new Untimeout(
                    Action: moderationAction,
                    TargetId: targetUserId,
                    Target: targetUserLogin,
                    CreatedAt: createdAt.Value,
                    InitiatorId: createdByUserId,
                    Initiator: createdBy);
                return true;

            default:
                result = null;
                return false;
        }
    }

    private static string? Get(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;

    private static string? NullIfEmpty(string value)
        => value is {Length: > 0} ? value : null;
}
