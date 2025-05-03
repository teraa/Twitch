using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.Tmi;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTmiService(
        this IServiceCollection services,
        Action<TmiServiceOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // const string key = "TmiService";

        services
            .AddTextWebSocketService(options => options.Uri = new Uri("wss://irc-ws.chat.twitch.tv:443"))
            .AddSingleton<ITmiService, TmiService>()
            .AddHostedService(sp => sp.GetRequiredService<ITmiService>())
            .AddTextWebSocketEventHandler<Ws.Events.ConnectedEvent, ConnectedEventHandler>()
            .AddTextWebSocketEventHandler<Ws.Events.MessageReceivedEvent, MessageReceivedEventHandler>();


        return services;
    }

    public static IServiceCollection AddTmiEventHandler<TEvent, [MeansImplicitUse] THandler>(
        this IServiceCollection services)
        where THandler : class, ITmiEventHandler<TEvent>
        where TEvent : ITmiEvent
    {
        return services
            .AddScoped<ITmiEventHandler<TEvent>, THandler>();
    }
}
