namespace Teraa.Twitch.Helix;

internal sealed class AuthHeadersHandler : DelegatingHandler
{
    private readonly IHelixApiAuthProvider _authProvider;

    public AuthHeadersHandler(IHelixApiAuthProvider authProvider)
    {
        _authProvider = authProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = await _authProvider.GetAuthHeader(cancellationToken);
        request.Headers.Add("Client-Id", await _authProvider.GetClientIdAsync(cancellationToken));
        return await base.SendAsync(request, cancellationToken);
    }
}
