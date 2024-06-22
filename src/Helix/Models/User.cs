using JetBrains.Annotations;

namespace Teraa.Twitch.Helix.Models;

[PublicAPI]
public record User(
    string Id,
    string Login,
    string DisplayName,
    string Type,
    string BroadcasterType,
    string Description,
    string ProfileImageUrl,
    string OfflineImageUrl,
    DateTimeOffset CreatedAt,
    string? Email
);
