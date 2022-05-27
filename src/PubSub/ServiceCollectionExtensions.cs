using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.PubSub;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPubSubService(this IServiceCollection services, Action<PubSubServiceOptions>? configureOptions = null)
    {
        services.TryAddTransient<IWsClient, WsClient>();
        services.TryAddSingleton<PubSubService>();
        services.AddHostedService(sp => sp.GetRequiredService<PubSubService>());

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }
}
