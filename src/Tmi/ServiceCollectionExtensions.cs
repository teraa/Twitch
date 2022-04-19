using Microsoft.Extensions.DependencyInjection;

namespace Teraa.Twitch.Tmi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTmiService(this IServiceCollection services)
    {
        services.AddSingleton<IWsClient, WsClient>();
        services.AddSingleton<TmiServiceOptions>();
        services.AddSingleton<TmiService>();
        services.AddHostedService(sp => sp.GetRequiredService<TmiService>());
        return services;
    }
}
