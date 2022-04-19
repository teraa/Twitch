using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.PubSub;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPubSubService(this IServiceCollection services)
    {
        services.AddTransient<IWsClient, WsClient>();
        services.AddSingleton<PubSubServiceOptions>();
        services.AddSingleton<PubSubService>();
        services.AddHostedService(sp => sp.GetRequiredService<PubSubService>());
        return services;
    }
}
