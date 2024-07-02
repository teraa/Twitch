using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.Tmi;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTmiService(this IServiceCollection services, Action<TmiServiceOptions>? configureOptions = null)
    {
        services.TryAddTransient<IWsClient, WsClient>();
        services.TryAddSingleton<TmiService>();
        services.AddHostedService(sp => sp.GetRequiredService<TmiService>());
        services.TryAddSingleton<ITmiClient>(sp => sp.GetRequiredService<TmiService>());

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }
}
