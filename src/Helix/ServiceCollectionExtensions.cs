using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Teraa.Twitch.Helix;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddHelixApi(this IServiceCollection services)
    {
        var builder = services
            .AddTransient<AuthHeadersHandler>()
            .AddRefitClient<IHelixApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer
                (
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    }
                ),
                CollectionFormat = CollectionFormat.Multi,
            })
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://api.twitch.tv/helix"))
            .AddHttpMessageHandler<AuthHeadersHandler>();

        return builder;
    }

    public static IHttpClientBuilder AddHelixApi<[MeansImplicitUse] THelixApiAuthProvider>(
        this IServiceCollection services)
        where THelixApiAuthProvider : class, IHelixApiAuthProvider
    {
        var builder = services
            .AddTransient<IHelixApiAuthProvider, THelixApiAuthProvider>()
            .AddHelixApi();

        return builder;
    }
}
