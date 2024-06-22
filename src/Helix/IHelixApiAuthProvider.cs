using System.Net.Http.Headers;

namespace Teraa.Twitch.Helix;

public interface IHelixApiAuthProvider
{
    ValueTask<string> GetClientIdAsync(CancellationToken cancellationToken);
    ValueTask<AuthenticationHeaderValue> GetAuthHeader(CancellationToken cancellationToken);
}
