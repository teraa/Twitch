using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Teraa.Twitch.Ws.Events;

namespace Teraa.Twitch.Ws;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTextWebSocketService(
        this IServiceCollection services,
        Action<TextWebSocketServiceOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services
            .AddSingleton<ITextWebSocketService, TextWebSocketService>()
            .AddTransient<ITextWebSocketClient, TextWebSocketClient>()
            .AddOptions<TextWebSocketClientOptions>();

        return services;
    }

    public static IServiceCollection AddTextWebSocketEventHandler<TEvent, [MeansImplicitUse] THandler>(
        this IServiceCollection services)
        where THandler : class, ITextWebSocketEventHandler<TEvent>
        where TEvent : ITextWebSocketEvent
    {
        return services
            .AddScoped<ITextWebSocketEventHandler<TEvent>, THandler>();
    }
}
