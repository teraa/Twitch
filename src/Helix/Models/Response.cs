using JetBrains.Annotations;

namespace Teraa.Twitch.Helix.Models;

[PublicAPI]
public record Response<T>(IReadOnlyList<T> Data);
