using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Teraa.Twitch.PubSub;

namespace Teraa.Twitch.Tmi;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTmiService(this IServiceCollection services)
    {
        services.AddTransient<IWsClient, WsClient>();
        services.AddSingleton<TmiServiceOptions>();
        services.AddSingleton<TmiService>();
        services.AddHostedService(sp => sp.GetRequiredService<TmiService>());
        return services;
    }

    public static IServiceCollection AddPubSubService(this IServiceCollection services)
    {
        services.AddTransient<IWsClient, WsClient>();
        services.AddSingleton<PubSubServiceOptions>();
        services.AddSingleton<PubSubService>();
        services.AddHostedService(sp => sp.GetRequiredService<PubSubService>());
        return services;
    }
}
