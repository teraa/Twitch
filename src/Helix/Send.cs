using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Http.Extensions;

namespace Teraa.Twitch.Helix;

public static class Send
{
    [PublicAPI]
    public record Request<TResponse>(
        HttpMethod HttpMethod,
        string Path,
        Action<QueryBuilder>? QueryBuilderOptions,
        string Token,
        string ClientId,
        Action<HttpRequestMessage>? RequestOptions
    ) : IRequest<TResponse>;

    public class Handler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : Request<TResponse>
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public Handler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient("Helix");
            var uriBuilder = new UriBuilder(new Uri(httpClient.BaseAddress!, request.Path));

            if (request.QueryBuilderOptions is { })
            {
                var queryBuilder = new QueryBuilder();
                request.QueryBuilderOptions(queryBuilder);
                uriBuilder.Query = queryBuilder.ToString();
            }

            using var httpRequest = new HttpRequestMessage(request.HttpMethod, uriBuilder.Uri);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.Token);
            httpRequest.Headers.Add("Client-Id", request.ClientId);
            request.RequestOptions?.Invoke(httpRequest);

            using var httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
            Debug.Assert(response is not null);

            return response;
        }
    }
}
