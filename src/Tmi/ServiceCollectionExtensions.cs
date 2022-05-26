﻿using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.Tmi;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTmiService(this IServiceCollection services)
    {
        services.TryAddTransient<IWsClient, WsClient>();
        services.TryAddSingleton<TmiServiceOptions>();
        services.TryAddSingleton<TmiService>();
        services.AddHostedService(sp => sp.GetRequiredService<TmiService>());
        return services;
    }
}
