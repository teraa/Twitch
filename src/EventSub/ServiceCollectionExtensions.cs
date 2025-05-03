using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.EventSub;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventSubService(
        this IServiceCollection services,
        Action<EventSubServiceOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // const string key = "EventSubService";

        services
            .AddTextWebSocketService(options => options.Uri = new Uri("wss://irc-ws.chat.twitch.tv:443"))
            .AddSingleton<IEventSubService, EventSubService>()
            .AddHostedService(sp => sp.GetRequiredService<IEventSubService>());


        return services;
    }

    public static IServiceCollection AddEventSubEventHandler<TEvent, [MeansImplicitUse] THandler>(
        this IServiceCollection services)
        where THandler : class, IEventSubEventHandler<TEvent>
        where TEvent : IEventSubEvent
    {
        return services
            .AddScoped<IEventSubEventHandler<TEvent>, THandler>();
    }
}
