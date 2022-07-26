using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Teraa.Twitch.Helix;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixService(this IServiceCollection services, Action<HelixServiceOptions>? configureOptions = null)
    {
        services.AddHttpClient("Helix", (serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("https://api.twitch.tv/helix/");
        });

        services.AddMediatR(typeof(HelixService));
        services.AddTransient(typeof(IRequestHandler<,>), typeof(Send.Handler<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehaviour<,>));
        services.AddValidatorsFromAssemblyContaining<HelixService>();

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }
}
