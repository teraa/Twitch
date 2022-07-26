// See https://aka.ms/new-console-template for more information

using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Teraa.Twitch.Helix;
using Teraa.Twitch.Helix.Users;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection()
    .AddLogging(configure => configure.AddSerilog())
    .AddHelixService(options =>
    {
        options.ClientId = config["Twitch:ClientId"];
    })
    .BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true,
    });

var sc = new ServiceCollection();

using var scope = services.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
var response = await mediator.Send(new Get.Query(Token: config["Twitch:Token"], Logins: new[] {"tera_"}));

_ = response;
