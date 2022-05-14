using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Teraa.Twitch.PubSub.Messages.ChannelUnbanRequests;

public static class Parser
{
    public static bool TryParse(JsonDocument input, [NotNullWhen(true)] out IUnbanRequest? result)
    {
        if (!input.RootElement.TryGetProperty("type", out var typeElement) ||
            !input.RootElement.TryGetProperty("data", out var data))
        {
            result = null;
            return false;
        }

        var type = typeElement.GetString();
        var id = data.GetPropertyString("id");
        var requesterId = data.GetPropertyString("requester_id");
        var requesterLogin = data.GetPropertyString("requester_login");

        if (id is null ||
            requesterId is null ||
            requesterLogin is null)
        {
            result = null;
            return false;
        }

        switch (type)
        {
            case "create_unban_request":
            {
                var channelId = data.GetPropertyString("channel_id");
                var createdAt = data.GetPropertyDateTimeOffset("created_at");
                var requesterMessage = data.GetPropertyString("requester_message");
                var requesterProfileImage = data.GetPropertyString("requester_profile_image");

                if (channelId is null ||
                    !createdAt.HasValue ||
                    requesterMessage is null ||
                    requesterProfileImage is null)
                    break;

                result = new CreateUnbanRequest(
                    ChannelId: channelId,
                    CreatedAt: createdAt.Value,
                    Id: id,
                    RequesterId: requesterId,
                    Requester: requesterLogin,
                    RequesterMessage: requesterMessage,
                    RequesterProfileImageUrl: requesterProfileImage);

                return true;
            }

            case "update_unban_request":
            {
                var resolverId = data.GetPropertyString("resolver_id");
                var resolverLogin = data.GetPropertyString("resolver_login");
                var resolverMessage = data.GetPropertyString("resolver_message");
                var status = data.GetPropertyString("status");

                if (resolverId is null ||
                    resolverLogin is null ||
                    resolverMessage is null ||
                    status is null)
                    break;

                result = new UpdateUnbanRequest(
                    Id: id,
                    RequesterId: requesterId,
                    Requester: requesterLogin,
                    ResolverId: resolverId,
                    Resolver: resolverLogin,
                    ResolverMessage: resolverMessage,
                    Status: status);

                return true;
            }
        }

        result = null;
        return false;
    }
}
