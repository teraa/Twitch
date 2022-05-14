namespace Teraa.Twitch.PubSub.Messages.ChannelUnbanRequests;

public interface IUnbanRequest
{
    string Id { get; }
    string RequesterId { get; }
    string Requester { get; }
}

public record CreateUnbanRequest(
    string ChannelId,
    DateTimeOffset CreatedAt,
    string Id,
    string RequesterId,
    string Requester,
    string RequesterMessage,
    string RequesterProfileImageUrl
) : IUnbanRequest;

public record UpdateUnbanRequest(
    string Id,
    string RequesterId,
    string Requester,
    string ResolverId,
    string Resolver,
    string ResolverMessage,
    string Status
) : IUnbanRequest;
